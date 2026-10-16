#!/usr/bin/env bash
set -euo pipefail
version=${1:?Expected published version}
[[ $version =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || exit 2
source=${2:-https://api.nuget.org/v3/index.json}
root=$(cd "$(dirname "$0")/../.." && pwd)
scratch=$(mktemp -d "${TMPDIR:-/tmp}/qyl-testing-consumer.XXXXXXXX")
trap 'rm -rf "$scratch"' EXIT
cp "$root/global.json" "$scratch/global.json"
cat >"$scratch/smoke.csproj" <<XML
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup><ItemGroup><PackageReference Include="Qyl.Telemetry.Testing" Version="$version"/><PackageReference Include="OpenTelemetry" Version="1.20.0"/><PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.20.0"/></ItemGroup></Project>
XML
cat >"$scratch/NuGet.Config" <<XML
<configuration><packageSources><clear/><add key="candidate" value="$source"/><add key="public" value="https://api.nuget.org/v3/index.json"/></packageSources></configuration>
XML
cat >"$scratch/Program.cs" <<'CS'
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using RpcStatus = Google.Rpc.Status;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Qyl.Telemetry.Testing;
using SpanKind = Qyl.Telemetry.SemanticConventions.SpanKind;

const string scope = "release.proof";
foreach (var transport in new[] { OtlpTransport.HttpProtobuf, OtlpTransport.Grpc })
{
var trace = await QylTest.TraceAsync(capture =>
{
    var resource = ResourceBuilder.CreateEmpty().AddAttributes(new Dictionary<string, object>
        { ["service.name"] = "release-consumer", [capture.Partition.ResourceAttribute] = capture.Partition.Value },
        "https://opentelemetry.io/schemas/1.44.0");
    using var source = new ActivitySource(scope, "1.0.0");
    using var traces = Sdk.CreateTracerProviderBuilder().AddSource(scope).SetResourceBuilder(resource)
        .AddOtlpExporter(o => Configure(o, capture, "traces", transport)).Build();
    using var meter = new Meter(scope, "1.0.0");
    using var metrics = Sdk.CreateMeterProviderBuilder().AddMeter(scope).SetResourceBuilder(resource)
        .AddOtlpExporter(o => Configure(o, capture, "metrics", transport)).Build();
    using var logs = LoggerFactory.Create(b => b.AddOpenTelemetry(o =>
    {
        o.Version = "1.0.0";
        o.SchemaUrl = "https://opentelemetry.io/schemas/1.44.0";
        o.SetResourceBuilder(resource).AddOtlpExporter(e => Configure(e, capture, "logs", transport));
    }));
    using var activity = source.StartActivity("release.operation", ActivityKind.Server)
        ?? throw new InvalidOperationException("The tracer did not subscribe to the application source.");
    activity.SetStatus(ActivityStatusCode.Ok).SetTag("http.request.method", "POST")
        .SetTag("checkout.sequence", 9_007_199_254_740_993L).SetTag("checkout.accepted", false)
        .SetTag("checkout.labels", new[] { "first", "second" });
    LoggerMessage.Define<int>(LogLevel.Information, new EventId(19, "OrderCreated"), "Created {OrderId}")(
        logs.CreateLogger(scope), 41, null);
    meter.CreateCounter<long>("release.proof.counter", "{order}").Add(7);
    Require(metrics.ForceFlush(), "Metric flush failed.");
    activity.Stop();
    return Task.CompletedTask;
}, t => t.Spans.Count == 1 && t.Logs.Count == 1 && t.Metrics.Count > 0,
    maxPayloadBytes: 16 * 1024 * 1024);
var span = trace.Spans.Single();
Require(span.TraceId == trace.TraceId && span.Name == "release.operation" && span.Kind == SpanKind.Server
    && span.Status == ActivityStatusCode.Ok && span.Scope is { Name: scope, Version: "1.0.0" }
    && span.Resource.SchemaUrl == "https://opentelemetry.io/schemas/1.44.0"
    && span.GetString("http.request.method") == "POST"
    && span.GetInt64("checkout.sequence") == 9_007_199_254_740_993L
    && span.GetBoolean("checkout.accepted") is false
    && span.GetStrings("checkout.labels") is { } labels && labels.SequenceEqual(new[] { "first", "second" }),
    "Wrong exported span identity, scope or native application attributes.");
var log = trace.Logs.Single();
Require(log.TraceId == trace.TraceId && log.SpanId == span.SpanId && log.EventName == "OrderCreated"
    && log.SeverityNumber == 9 && log.Scope is { Name: scope, Version: "1.0.0", SchemaUrl: "https://opentelemetry.io/schemas/1.44.0" }
    && log.Attributes.TryGetValue("OrderId", out var orderId) && orderId is long and 41,
    "Wrong named log, correlation, severity or typed attributes.");
var exportedMetrics = trace.Metrics.Where(m => m.Metric.Name == "release.proof.counter").ToArray();
Require(exportedMetrics.Length > 0, "The counter was not exported.");
foreach (var metric in exportedMetrics)
Require(metric.Correlation == MetricCorrelation.ResourcePartition && metric.Metric.Scope?.Name == scope
    && metric.Metric.Unit == "{order}" && metric.Metric.Data is SumData { IsMonotonic: true } sum
    && sum.Points.Single().Value is long and 7, "Wrong exported counter, unit, scope or point value.");
using (var archive = JsonSerializer.SerializeToDocument(trace.Capture))
{
    var counter = archive.RootElement.GetProperty("Metrics").EnumerateArray()
        .First(m => m.GetProperty("Name").GetString() == "release.proof.counter").GetProperty("Data");
    Require(counter.GetProperty("kind").GetString() == "sum" &&
        counter.GetProperty("Points")[0].GetProperty("Value").GetInt64() == 7,
        "The diagnostic archive lost the metric shape or exact point value.");
}
Require(trace.Capture.Requests.Count >= 3 && trace.Capture.Requests.All(r => r.Transport == transport && !r.Payload.IsEmpty),
    "Original protobuf export payloads and transport were not retained.");
Require(trace.Capture.Receipts.All(r => r.Transport == transport && r.Rejected == 0 && r.Error is null),
    "Wrong transport or unexpected export rejection.");
var audit = RegistryConformance.Check(trace.Capture);
Require(audit.IsConformant && audit.CustomMetrics.Contains("release.proof.counter"), "Registry audit rejected valid custom telemetry.");

Console.WriteLine($"Package consumer verified {transport} correlated signals, native application attributes, metric JSON and original payloads.");
}

await using var receiver = await OtlpReceiver.StartAsync();
using var http = new HttpClient();
using var malformed = new ByteArrayContent([0x80]);
malformed.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
using var rejected = await http.PostAsync(new Uri(receiver.Endpoint, "v1/traces"), malformed);
Require(rejected.StatusCode == HttpStatusCode.BadRequest, "Malformed protobuf was accepted.");
var receipt = receiver.Snapshot().Receipts.Single();
Require(receipt is { Signal: "traces", Accepted: 0, Error: not null }, "Missing malformed-request evidence.");
Require(rejected.Content.Headers.ContentType?.MediaType == "application/x-protobuf", "Wrong error response encoding.");
var error = RpcStatus.Parser.ParseFrom(await rejected.Content.ReadAsByteArrayAsync());
Require(error.Code == (int)Google.Rpc.Code.InvalidArgument && error.Message == receipt.Error,
    "Malformed protobuf did not return a useful protobuf Status.");
Require(receiver.Snapshot().Requests.Single().Payload.Span.SequenceEqual(new byte[] { 0x80 }),
    "Malformed original payload was not retained.");
Console.WriteLine("Package consumer also verified malformed HTTP protobuf and its decoded error Status.");

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void Configure(OtlpExporterOptions options, QylCapture capture, string signal, OtlpTransport transport)
{
    options.Protocol = transport == OtlpTransport.Grpc ? OtlpExportProtocol.Grpc : OtlpExportProtocol.HttpProtobuf;
    options.Endpoint = transport == OtlpTransport.Grpc ? capture.GrpcEndpoint : new Uri(capture.Endpoint, "v1/" + signal);
    options.ExportProcessorType = ExportProcessorType.Simple;
}
CS
NUGET_PACKAGES="$scratch/packages" dotnet restore "$scratch/smoke.csproj" --configfile "$scratch/NuGet.Config" --no-http-cache
NUGET_PACKAGES="$scratch/packages" dotnet run --project "$scratch/smoke.csproj" --configuration Release --no-restore
