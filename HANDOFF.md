# Qyl.Telemetry.Testing 1.0.0 candidate

The primary owns this repository. All agents are finished. SemanticConventions 11.0.0 is publicly indexed and its producer release is VERIFIED at commit 2142f58. Testing restores that public package from a fresh public-only cache. OpenTelemetry test exporters are now 1.20.0; the final native Pack gate passed in artifacts/otel120-public-pack-final.log. Testing1.0.0 is not published or tagged yet.

## Implementation

The receiver captures actual loopback OTLP/HTTP protobuf/gzip traces, named logs and all five metric shapes. It preserves exact typed values, resource/scope schema and loss metadata, flags, nanoseconds, ordered events/links and exemplars. Official proto 1.11.1 sources, hashes and licensing remain intact. Canonical span/resource/scope models come from the public conventions package.

Native MSTest integration delegates to the original attributes/results and keeps global hooks in the consuming assembly. Owned child processes verify data rows, failures, durations and actual discovery locations. Trace selection never filters raw capture; metric attribution needs an explicit partition or exemplar. Definition-scoped conformance and registry audits have different contracts: the latter checks observed types, units and lifecycle facts, lists valid custom names separately, and reports deprecation as improvement advice. It never invents scope ownership, global required attributes, closed enums or SDK instrument identity from aggregations.

The build now uses Fallout's native injected solution and no CA1822 suppression. Packed consumers exercise actual SDK traces, named logs and partitioned counters, plus malformed-request evidence; they pin the repository SDK in the isolated directory. Native release runs that consumer before publication as well as after indexing.

## Measurement and verification

The public-only run passed 160 tests but failed the original blanket 100% gate: full coverage was 4603/7479 lines and 493/1608 branches. Matching local PDBs caused SemanticConventions to be instrumented, whereas the earlier local candidate's PDB did not match. Exact CodeView/PDB evidence is artifacts/catalog-symbol-provenance-verified.log. The full failed report is retained under artifacts/public-coverage-with-dependency-symbols. No symbol or binary was removed or modified to improve coverage.

On 2026-10-16 the owner explicitly removed blanket library100% requirements in favor of qyl business rules, invariants and useful tests. The native build retains all unfiltered measurements through the published DotCov parser/report API; it fails unreadable or missing coverage, failing tests and any INFO-or-higher inspection finding. It does not label informational coverage as a threshold pass.

Testing's 1110/1110 physical lines and 452/452 branches are a package breakdown, not the combined report. Raw Cobertura has 1185 class-line entries, 1169 hit. Each of 16 unhit generated parser-factory entries shares a hit physical initializer line. This does not claim those factory bodies executed. Both raw and physical counts remain visible. No coverage exclusion or constructor-touching test was added.

The reviewed source ledger is eng/test-review.json and TEST-REVIEW.md. The native SDK 1.20 Pack gate and packed consumer passed; current evidence is recorded in eng/verification.json. The older 127-case source-archive run only covers its earlier source. Vendored proto whitespace is preserved verbatim.

## Remaining work

1. Commit and push the verified current source; release 1.0.0 through release.sh and wait for native VERIFIED.
2. Integrate the package into AutoInstrumentation's native MSTest demo lanes and the live prototype. No PR or worktree is needed.
