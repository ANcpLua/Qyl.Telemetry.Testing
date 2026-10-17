# Changelog

## 3.0.2

- Consume SemanticConventions 13.1.1, matching the Collector's current dependency. Captures continue to preserve original attributes without applying Collector persistence policy.
- Replace the README with a compact usage guide and remove obsolete handoff and duplicated test-review narratives. Keep historical verification evidence under eng/ and artifacts/.

## 3.0.1

- Restore checkout package paths to the caller's NuGet cache before removing the isolated release cache, including the runner probes and build host. Preserve the original release failure; if cleanup restore fails, retain the cache and report failure instead of leaving dangling package paths.

## 3.0.0

- Keep the testing receiver's 16 MiB default. Add a positive `maxPayloadBytes` startup argument shared by HTTP and gRPC, also available through `QylTest.TraceAsync`. Create receivers through the startup factory.
- Use ASP.NET Core request decompression and body-size limits for HTTP, preserving decodable OTLP 413 errors. Bound both wire bytes and decoded output, including compressed requests without `Content-Length`.
- Retain bounded gRPC per-message decompression and `ResourceExhausted` overflow responses. Report the configured byte limit in HTTP and gRPC diagnostics; verify default and custom exact/overflow boundaries with plain and gzip requests.

## 2.0.0

- Receive traces, logs and metrics over OTLP/gRPC on a separate dynamic loopback port. HTTP and gRPC share decoding, partial-success diagnostics and original payload capture; exports identify their transport.
- Preserve original gRPC message bytes, including unknown fields and noncanonical wire encodings. Bound gzip decompression to 16 MiB while reading, honor cancellation and close both listeners on disposal.
- Verify both transports with real OpenTelemetry 1.20.0 exporters and package consumers, plus gRPC wire, error, boundary and lifecycle tests.

- Consume SemanticConventions 13.0.0. Captured attributes retain their original names and native types; removed stringifying `App` views are replaced by `SpanRecord.Attributes` and its typed getters.
- Verify real SDK exports with application-owned keys and historical `qyl.app` names without imposing the obsolete string template on them.

## 1.1.2

- Honor cancellation before evaluating an evidence condition, including when the condition would already pass. Preserve the caller's cancellation token and check the deadline during continuous arrivals.

## 1.1.1

- Derive service calls from unique span identities so retransmitting an export cannot invent calls. Keep every raw span observation and original export, and retain distinct calls between the same services.

## 1.1.0

- Consume SemanticConventions 12.0.0, including the verified Oracle duration wire-type correction.

- Return typed protobuf Status errors and useful diagnostic messages for malformed, oversized and unsupported OTLP/HTTP requests, preserving the request's supported response encoding.
- Handle profiling-only string-table references as non-fatal empty values with retained warnings, rather than dereferencing a missing map. Keep literal keys when present and ignore unresolved indexed keys.
- Preserve each uncompressed protobuf export before decoding, including unknown fields and records rejected by the decoder. Snapshot membership remains stable.
- Serialize all metric data shapes with their complete points and a kind discriminator, retaining exact 64-bit numbers in diagnostic JSON.

## 1.0.0

- Capture OTLP/HTTP protobuf traces, logs and all five metric shapes in an owned in-process receiver.
- Keep exact span/resource/scope metadata, ordered events and links, typed values and log event names.
- Isolate native MSTest invocations and attribute metrics only through an explicit resource partition or exemplar.
- Derive service calls and database identities from captured spans; check explicit semantic-convention definitions and
  audit observed registry attribute types, metric units and lifecycle metadata without rejecting custom names or
  inventing global requirements.
- Verify packed consumers with real SDK traces, named logs and partitioned metrics, retaining malformed-request
  evidence.
