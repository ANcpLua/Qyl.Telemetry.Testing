# Changelog

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
