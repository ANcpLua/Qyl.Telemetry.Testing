# Changelog

## 1.0.0

- Capture OTLP/HTTP protobuf traces, logs and all five metric shapes in an owned in-process receiver.
- Keep exact span/resource/scope metadata, ordered events and links, typed values and log event names.
- Isolate native MSTest invocations and attribute metrics only through an explicit resource partition or exemplar.
- Derive service calls and database identities from captured spans; check explicit semantic-convention definitions without claiming registry-wide conformance.
