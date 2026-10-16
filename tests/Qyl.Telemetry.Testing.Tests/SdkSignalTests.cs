using System.Diagnostics;
using System.Diagnostics.Metrics;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SpanKind = Qyl.Telemetry.SemanticConventions.SpanKind;

namespace Qyl.Telemetry.Testing.Tests;

[TestClass]
public sealed class SdkSignalTests
{
    private static readonly Action<ILogger, int, Exception?> OrderCreated =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(19, "OrderCreated"), "Created {OrderId}");

    [UsedImplicitly] public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task Official_sdk_exports_trace_named_log_and_partitioned_counter()
    {
        var sourceName = "sdk-" + Guid.NewGuid().ToString("N");
        var result = await QylTest.TraceAsync(test =>
            {
                var resource = ResourceBuilder.CreateEmpty().AddAttributes(new Dictionary<string, object>
                        { ["service.name"] = "sdk-service", [test.Partition.ResourceAttribute] = test.Partition.Value },
                    "https://opentelemetry.io/schemas/1.44.0");
                using var source = new ActivitySource(sourceName, "2.0.1");
                using var traces = Sdk.CreateTracerProviderBuilder().AddSource(sourceName).SetResourceBuilder(resource)
                    .AddOtlpExporter(o => Configure(o, test.Endpoint, "traces")).Build();
                using var meter = new Meter(sourceName, "2.0.1");
                using var metrics = Sdk.CreateMeterProviderBuilder().AddMeter(sourceName).SetResourceBuilder(resource)
                    .AddOtlpExporter(o => Configure(o, test.Endpoint, "metrics")).Build();
                using var logs = LoggerFactory.Create(b => b.AddOpenTelemetry(o => o.SetResourceBuilder(resource)
                    .AddOtlpExporter(e => Configure(e, test.Endpoint, "logs"))));
                using var activity = source.CreateActivity("sdk.operation", ActivityKind.Server)!.Start();
                activity.SetStatus(ActivityStatusCode.Ok).SetTag("http.request.method", "POST");
                OrderCreated(logs.CreateLogger(sourceName), 41, null);
                meter.CreateCounter<long>("orders.created", "{order}", "Created orders").Add(7);
                Assert.IsTrue(metrics.ForceFlush());
                activity.Stop();
                return Task.CompletedTask;
            }, t => t.Spans.Count == 1 && t.Logs.Count == 1 && t.Metrics.Count > 0,
            cancellationToken: TestContext.CancellationToken);
        var span = Assert.ContainsSingle(result.Spans);
        Assert.AreEqual(result.TraceId, span.TraceId);
        Assert.AreEqual("sdk.operation", span.Name);
        Assert.AreEqual(SpanKind.Server, span.Kind);
        Assert.AreEqual(ActivityStatusCode.Ok, span.Status);
        Assert.AreEqual(sourceName, span.Scope?.Name);
        Assert.AreEqual("https://opentelemetry.io/schemas/1.44.0", span.Resource.SchemaUrl);
        var log = Assert.ContainsSingle(result.Logs);
        Assert.AreEqual(result.TraceId, log.TraceId);
        Assert.AreEqual(span.SpanId, log.SpanId);
        Assert.AreEqual("OrderCreated", log.EventName);
        Assert.AreEqual(9, log.SeverityNumber);
        Assert.AreEqual(sourceName, log.Scope?.Name);
        Assert.AreEqual("sdk-service", log.Resource.Attributes["service.name"]);
        Assert.AreEqual("https://opentelemetry.io/schemas/1.44.0", log.Resource.SchemaUrl);
        var metric = result.Metrics.First(m => m.Metric.Name == "orders.created");
        Assert.AreEqual(MetricCorrelation.ResourcePartition, metric.Correlation);
        Assert.AreEqual(sourceName, metric.Metric.Scope?.Name);
        Assert.AreEqual("{order}", metric.Metric.Unit);
        Assert.AreEqual("https://opentelemetry.io/schemas/1.44.0", metric.Metric.Resource.SchemaUrl);
        var sum = Assert.IsInstanceOfType<SumData>(metric.Metric.Data);
        Assert.IsTrue(sum.IsMonotonic);
        Assert.AreEqual(7L, Assert.ContainsSingle(sum.Points).Value);
    }

    private static void Configure(OtlpExporterOptions options, Uri endpoint, string signal)
    {
        options.Protocol = OtlpExportProtocol.HttpProtobuf;
        options.Endpoint = new Uri(endpoint, "v1/" + signal);
        options.ExportProcessorType = ExportProcessorType.Simple;
    }
}