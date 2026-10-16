# Qyl.Telemetry.Testing 1.1.0 published and verified

The primary continues alone. Release source 7a7d46f consumes public SemanticConventions 12.0.0 (b7285bb).
Native release completed: all 168 tests passed with zero skips, zero INFO-or-higher inspections, unfiltered coverage,
and the packed consumer smoke. NuGet indexing and the public-only live receiver consumer passed; the native log ends
VERIFIED (observed at 2026-10-16T18:52:37Z). Evidence: artifacts/release-1.1.0-{driver,verify}.log and
artifacts/semconv12-diagnostics-{pack-2,smoke}.log. The DLL restored for the AutoInstrumentation full gate exactly
matches the public 1.1.0 DLL. The original owner formatting is preserved.

Version 1.1.0 adds complete polymorphic metric JSON, uncompressed original protobuf exports, official Google.Rpc.Status
HTTP errors and nonfatal profiling string/key references with retained warnings. The unfiltered graph measures
4650/7529 lines and 495/1612 branches. Eager oversized HTTP/1.1 uploads can still surface a broken pipe while Kestrel
closes the rejected body; the declared-size error response is verified using Expect100-continue. Earlier failed
attempts remain in artifacts. The receiver's HTTP/JSON support is for diagnostic errors, not JSON OTLP ingestion.

## Implementation

The receiver captures actual loopback OTLP/HTTP protobuf/gzip traces, named logs and all five metric shapes. It
preserves exact typed values, resource/scope schema and loss metadata, flags, nanoseconds, ordered events/links and
exemplars. Official proto 1.11.1 sources, hashes and licensing remain intact. Canonical span/resource/scope models come
from the public conventions package.

Native MSTest integration delegates to the original attributes/results and keeps global hooks in the consuming assembly.
Owned child processes verify data rows, failures, durations and actual discovery locations. Trace selection never
filters raw capture; metric attribution needs an explicit partition or exemplar. Definition-scoped conformance and
registry audits have different contracts: the latter checks observed types, units and lifecycle facts, lists valid
custom names separately, and reports deprecation as improvement advice. It never invents scope ownership, global
required attributes, closed enums or SDK instrument identity from aggregations.

The build now uses Fallout's native injected solution and no CA1822 suppression. Packed consumers exercise actual SDK
traces, named logs and partitioned counters, plus malformed-request evidence; they pin the repository SDK in the
isolated directory. Native release runs that consumer before publication as well as after indexing.

## Measurement and verification

The public-only run passed 160 tests but failed the original blanket 100% gate: full coverage was 4603/7479 lines and
493/1608 branches. Matching local PDBs caused SemanticConventions to be instrumented, whereas the earlier local
candidate's PDB did not match. Exact CodeView/PDB evidence is artifacts/catalog-symbol-provenance-verified.log. The full
failed report is retained under artifacts/public-coverage-with-dependency-symbols. No symbol or binary was removed or
modified to improve coverage.

On 2026-10-16 the owner explicitly removed blanket library 100% requirements in favor of qyl business rules, invariants
and useful tests. The native build retains all unfiltered measurements through the published DotCov parser/report API;
it fails unreadable or missing coverage, failing tests and any INFO-or-higher inspection finding. It does not label
informational coverage as a threshold pass.

Testing's 1110/1110 physical lines and 452/452 branches are a package breakdown, not the combined report. Raw Cobertura
has 1185 class-line entries, 1169 hit. Each of 16 unhit generated parser-factory entries shares a hit physical
initializer line. This does not claim those factory bodies executed. Both raw and physical counts remain visible. No
coverage exclusion or constructor-touching test was added.

The reviewed source ledger is eng/test-review.json and TEST-REVIEW.md. The native SDK 1.20 Pack gate and packed consumer
passed; current evidence is recorded in eng/verification.json. The older 127-case source-archive run only covers its
earlier source. Vendored proto whitespace is preserved verbatim.

## Remaining work

1. Integrate the package into AutoInstrumentation's native MSTest demo lanes and the live prototype. No PR or worktree
   is needed.
