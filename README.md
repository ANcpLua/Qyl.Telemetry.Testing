# Qyl.Telemetry.Testing

Capture OTLP telemetry inside a .NET test and inspect what was exported. The receiver accepts traces, logs and metrics over OTLP/HTTP protobuf, including gzip. It keeps typed values, resource and scope metadata, ordered span events and links, named log events and all five metric shapes.

The package targets .NET 10 and uses the shared `Microsoft.AspNetCore.App` framework. Its native test integration uses MSTest 4.5.1 and Microsoft.Testing.Platform 2.5.1. Version 1.0.0 is being prepared; publication is a separate release step.

## Capture a trace

Configure the application's OTLP exporters with the endpoint supplied to the action. Each action has a W3C trace root and its own receiver. For the .NET OTLP exporter, use `new Uri(capture.Endpoint, "v1/traces")`, `"v1/logs"` or `"v1/metrics"` with `OtlpExportProtocol.HttpProtobuf`. Propagate `capture.TraceParent` when calling a separate service.

```csharp
var trace = await QylTest.TraceAsync(
    async capture =>
    {
        // Configure and start the application with capture.Endpoint.
        // Propagate capture.TraceParent in the request's traceparent header.
        await ExerciseApplicationAsync(capture.Endpoint, capture.TraceParent);
    },
    until: trace => trace.Spans.Any(span => span.Name == "checkout"),
    cancellationToken: TestContext.CancellationToken);

Assert.HasCount(1, trace.Services);
Assert.IsTrue(trace.Spans.Any(span => span.Name == "checkout"));
```

`ExerciseApplicationAsync` above is the application's own startup/request code. The receiver does not start a service or configure an exporter on its behalf. Stop spans and flush exporters before returning from the action. `TraceAsync` waits for the supplied evidence condition, with a ten-second default deadline. A missing condition times out; cancellation and exceptions remain failures.

`trace.Capture` contains every received record, including records from other trace IDs. `trace.Spans` and `trace.Logs` select the action's trace. `Services`, `Calls` and `StoresOf(service)` derive service and database relationships from those spans. `SessionIds` contains observed `session.id` values; `SessionId` is populated only when exactly one was observed. A trace ID is not a session ID.

## Native MSTest integration

Standalone `TraceAsync` needs no custom test class. To share a root across an entire MSTest invocation, replace `[TestClass]` with `[QylTelemetry]` and add these native lifecycle hooks to the **consuming test assembly**:

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

The wrapper delegates execution to the original `TestMethodAttribute`. Native data-row identity, source navigation, duration, assertions and exceptions remain MSTest's responsibility. Each invocation has a separate root. The hooks expose its trace ID as `TestContext.Properties[QylTestHooks.TraceIdProperty]`; they do not register themselves automatically from a referenced assembly.

## Metrics and conformance

A metric point usually has no trace ID. Add `capture.Partition.ResourceAttribute` and `capture.Partition.Value` to the exporting resource to explicitly identify this test's metrics, or export an exemplar carrying the trace ID. `trace.Metrics` identifies the matching point indexes and whether the evidence came from a resource partition or an exemplar. Other points remain visible in `Capture`; they are not attributed to the trace.

`trace.Conformance(definition, select)` checks an explicitly selected `SpanDefinition<TKind>` from Qyl.Telemetry.SemanticConventions. It checks span kind, required attributes, supported value types and deprecations in that definition. Missing conditional requirements are reported as unevaluated; an empty selection cannot pass. It is not a registry-wide audit, and it does not evaluate arbitrary condition text, allowed enum members or span-name patterns.

## Receiver contract

`OtlpReceiver.StartAsync()` exposes a loopback endpoint bound directly to an available port. `Snapshot()` returns received records; `AwaitAsync(condition, timeout, cancellationToken)` waits for a condition without polling sleeps. Dispose the receiver to stop the server and wake pending waits.

The wire model comes from OpenTelemetry protobuf v1.11.1. Generated DTOs are internal. Canonical span, resource and scope models come from Qyl.Telemetry.SemanticConventions. Values preserve their wire types, including signed 64-bit integers, doubles, bytes, arrays and maps. Unsigned timestamps and metric counts remain unsigned. Absent optional values remain absent.

The receiver supports protobuf, not OTLP JSON or gRPC. Unknown signal routes return 404; unsupported content types or encodings return 415. Requests and decompressed bodies are limited to 16 MiB. Malformed protobuf returns 400. Duplicate attribute keys and malformed IDs reject the affected record and return an OTLP partial-success count; accepted records remain captured. This in-process receiver is intended for tests, not production ingestion.

## Develop and release

Run `./build.sh Check` for the native suite, both 100% coverage gates and JetBrains InspectCode at INFO severity. `./build.sh Pack` includes those checks and packages the library. The deliberately failing probe project is built as a dependency and executed by passing parent tests that assert the exact failures, discovery locations and data-row identities.

Both the native DotCov summary and raw Cobertura report are retained. Their generated-code denominators can differ; the exact counts and remaining raw factory lines are recorded in `eng/verification.json`. The full test review is in [TEST-REVIEW.md](TEST-REVIEW.md). Proto provenance is recorded in `src/Qyl.Telemetry.Testing/Protos/upstream.json`; third-party licensing is in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

Releases use `./release.sh <version>` after changing the version and changelog. A release is complete only after the public package is indexed and its consumer smoke succeeds. Local candidates and passing source tests do not establish public availability.
