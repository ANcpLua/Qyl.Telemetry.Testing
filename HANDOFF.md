# Qyl.Telemetry.Testing 1.0.0 candidate

The package is implemented and locally verified. Publication is held. The consumer pins SemanticConventions 11.0.0, whose coordinated public release must be available before the push and a public-only restore check.

## Implementation

- Real loopback Kestrel receiver for OTLP/HTTP protobuf traces, logs and all five metric shapes; gzip and bounded request bodies.
- Official OpenTelemetry proto 1.11.1, pinned commit and eight source hashes. DTOs are internal; the Apache license is included in the NuGet archive.
- Canonical SemConv span/resource/scope records, ordered events/links, typed values and exact nanoseconds, flags, dropped counts and named logs.
- Trace-isolated capture, condition waits, disposal/cancellation, explicit resource-partition or exemplar metric attribution, service calls, stores and observed session IDs.
- Native MSTest wrapper preserving original attributes/results. Consumer global hooks live in its own test assembly. Real child runner tests prove failures, row identities, durations and source navigation.
- Explicit definition-scoped conformance. Invalid public requirement levels now throw an argument error; the earlier erroneous acceptance is reproduced in `artifacts/undefined-requirement-baseline.log`. Arbitrary conditional text, enum membership and span-name matching are not claimed as validated.

## Verification and provenance

The current exact results, source hashes and known raw coverage differences are in `eng/verification.json`, `eng/test-review.json` and `TEST-REVIEW.md`. The native full gate is `artifacts/final-pack-gate.log`; the separate clean staged-source run is `artifacts/source-audit.log`; the isolated NuGet consumer is `artifacts/local-package-smoke.log`.

The verified SemConv candidate is from commit 0c96f601cf81f767682ff5b0b4bed107806fd1f7, SHA256 8f1446f32eec84e06b89f5c01d576f64e1613a6b1639fc429d32e76af5a408c8. A later producer README-only candidate and the primary's catalog work are separate inputs; do not describe them as covered by these results. The final consumer check must restore the public 11.0.0 package and repeat relevant gates.

The temporary package cache is `/private/tmp/qyl-testing-nuget-final-20261016`. Its earlier location under `artifacts/` artificially expanded coverage to include the packaged SemConv assembly. Moving only that temporary cache outside the repository restored the native scope; no coverage filter or threshold was added or changed. The broad report is retained in `artifacts/coverage-with-in-repo-cache`.

The original shared method-group cache evidence is retained in `artifacts/delegate-cache-baseline`: source, DLL, PDB, raw Cobertura, build log and exact IL/PDB sequence points. Gauge and Sum originally loaded the same `<1>__Number` field twice. The current decoder shares their actual numeric-point transformation. There is no claim that the original gap was a DotCov parser bug.

The raw Cobertura report still lists 16 unexecuted generated protobuf parser factories. Native DotCov aggregation has different generated-code accounting. Both counts are recorded; do not replace raw counts with the native summary. No constructor-only tests or private-state resets were added for these factories.

One scoped CA1822 suppression is on the build host's Compile property: Fallout requires an instance Target; changing it to static made the real CLI fail with `Sequence contains no matching element`. The normal reflection-use annotation and the explicit justification preserve the required runtime contract. This does not suppress coverage.

The unchanged vendored protocol contains four upstream whitespace findings from `git diff --check`; all other staged files pass the check. Preserve the exact upstream bytes and hashes.

## Remaining work

1. Finish the coordinated public SemanticConventions 11 release and verify this consumer against that exact package from a fresh public-only cache.
2. Push the local main commit after that check. No PR or worktree is needed.
3. The primary decides any final generated-code coverage policy, integrates the package into the live prototype/AutoInstrumentation, and publishes 1.0.0 through `release.sh` when the stack is ready.

No package was published, no release tag was created and no Vitals credential value was read. Live prototype and AutoInstrumentation files were not changed by this assignment.
