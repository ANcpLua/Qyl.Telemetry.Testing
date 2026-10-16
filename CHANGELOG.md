# Changelog

## 1.0.0

- Capture OTLP/HTTP protobuf traces, logs and all five metric shapes in an owned in-process receiver.
- Keep exact span/resource/scope metadata, ordered events and links, typed values and log event names.
- Isolate native MSTest invocations and attribute metrics only through an explicit resource partition or exemplar.
- Derive service calls and database identities from captured spans; check explicit semantic-convention definitions and audit observed registry attribute types, metric units and lifecycle metadata without rejecting custom names or inventing global requirements.
- Verify packed consumers with real SDK traces, named logs and partitioned metrics, retaining malformed-request evidence.
