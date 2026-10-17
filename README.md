# Qyl.Telemetry.Testing

Capture exported traces, logs and metrics in .NET tests. Use a loopback OTLP receiver on its own,
or capture one operation with `QylTest.TraceAsync`. No running Collector is required.

Targets .NET 10, uses the shared `Microsoft.AspNetCore.App` framework, and integrates with native
MSTest 4.5.1. Version 3.0.2 consumes Qyl.Telemetry.SemanticConventions 13.1.1.

## Capture an operation

In a .NET 10 MSTest project:

```sh
dotnet add package Qyl.Telemetry.Testing --version 3.0.2
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol --version 1.20.0
```

```csharp
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;
using Qyl.Telemetry.Testing;

[TestClass]
public sealed class TelemetryTests
{
    [TestMethod]
    public async Task CapturesExportedSpan()
    {
        var trace = await QylTest.TraceAsync(capture =>
        {
            using var source = new ActivitySource("Example.Worker");
            using var provider = Sdk.CreateTracerProviderBuilder()
                .AddSource(source.Name)
                .AddOtlpExporter(options =>
                {
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                    options.Endpoint = new Uri(capture.Endpoint, "v1/traces");
                }).Build();
            using var activity = source.StartActivity("work")
                ?? throw new InvalidOperationException("Source was not subscribed.");
            activity.SetTag("work.items", 3L);
            return Task.CompletedTask;
        }, until: captured => captured.Spans.Any(span => span.Name == "work"));

        Assert.AreEqual(3L, trace.Spans.Single().GetInt64("work.items"));
    }
}
```

Stop activities and flush or dispose exporters before the action returns. For a separate application,
configure its exporters with the supplied endpoints and propagate `capture.TraceParent` in the request's
`traceparent` header. Missing evidence times out after ten seconds by default; exceptions and cancellation
remain failures. Each capture owns and disposes its receiver.

For gRPC, choose `OtlpExportProtocol.Grpc` and `capture.GrpcEndpoint`, without a signal path.
HTTP protobuf endpoints use `v1/traces`, `v1/logs` and `v1/metrics`.

## Inspect the evidence

- `trace.Spans` and `trace.Logs` select the operation's trace. Attributes retain their original names
  and native types; span events, links, log bodies and resource/scope metadata remain available.
- `trace.Metrics` requires an exemplar with the trace ID or an explicit resource partition. Add
  `capture.Partition.ResourceAttribute` and `capture.Partition.Value` to the exporting resource.
- `trace.Capture` retains all received records, including unrelated traces and repeated exports.
  `Requests` retains original protobuf payloads; `Receipts` records acceptance, rejection and warnings.
- `RegistryConformance.Check(trace.Capture)` checks observed registered types and units. Custom names
  remain valid and visible; deprecations are advice. `trace.Conformance(definition, select)` checks a
  selected span definition. Neither check treats empty evidence as a pass or invents missing conditions.

For an independently managed receiver:

```csharp
await using var receiver = await OtlpReceiver.StartAsync(
    maxPayloadBytes: 4 * 1024 * 1024);
// Configure exporters with receiver.Endpoint or receiver.GrpcEndpoint.
// Read receiver.Snapshot(), or await receiver.AwaitAsync(condition, timeout).
```

Both transports default to **16 MiB**; a positive `maxPayloadBytes` overrides both at startup.
`QylTest.TraceAsync` accepts the same argument. Select 4 MiB explicitly when matching the production
Collector's default gRPC limit. HTTP bounds encoded and decompressed bytes independently through
ASP.NET Core, including chunked gzip. gRPC bounds each encoded and decompressed message.
Overflow returns HTTP 413 with an OTLP Status or gRPC `ResourceExhausted`; malformed protobuf returns
HTTP 400 or gRPC `InvalidArgument`. Diagnostics include the configured byte limit.

The receiver accepts protobuf, including gzip, over HTTP and gRPC. It does not ingest OTLP JSON.
Malformed records produce partial-success diagnostics while valid records survive. Profiling-only
string-table references become empty or absent with visible warnings. This is a test receiver, not
production ingestion. Protocol sources and licensing are recorded in
[upstream.json](src/Qyl.Telemetry.Testing/Protos/upstream.json) and
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Share a native MSTest invocation root

Use `[QylTelemetry]` in place of `[TestClass]`, then declare these hooks in the consuming test assembly:

```csharp
[TestClass]
public static class TelemetryLifecycle
{
    [GlobalTestInitialize]
    public static void Initialize(TestContext context) => QylTestHooks.Initialize(context);

    [GlobalTestCleanup]
    public static void Cleanup(TestContext context) => QylTestHooks.Cleanup(context);
}
```

The wrapper delegates to the original method attribute, preserving native results, data rows and discovery.
Hooks expose the invocation trace ID through `TestContext.Properties[QylTestHooks.TraceIdProperty]`.

## Develop

`./build.sh Check` runs native tests, unfiltered informational coverage and JetBrains inspections.
`./build.sh Pack` adds packaging. Tests and inspections must pass; coverage has no blanket percentage target.
Release with `./release.sh <version>` after committing the version and changelog. Publication is verified
only when the package is indexed and the public consumer smoke passes.
