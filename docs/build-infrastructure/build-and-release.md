# Build graph and release evidence

## Consolidate responsibilities

Keep one Fallout build host and one supported entrypoint. The existing Testing
host lives at `eng/build/_build.csproj`; the prototype has a separate `build/`
host. Merge useful targets without preserving a second pipeline merely for
compatibility. Consult local Fallout source for its actual APIs and behavior.

| Stage | Responsibility |
| --- | --- |
| Unit tests | Pure assertions, filtering, policy and evidence-processing rules |
| Integration tests | Real OTLP transport, isolation, flushing and component interaction |
| Candidate packing | Produce the identified library package and verify package contents |
| End-to-end tests | Run relevant applications/scenarios against that candidate package |
| Public verification | Restore and exercise the published immutable package independently |

Classify cases by their actual dependencies. For example, a model-named test that
starts an HTTP receiver is not pure simply because of its filename. Preserve
native MSTest discovery, data rows, cancellation, outcomes and source locations.
Deliberately failing probes need their own child process and expected structured
results; do not add them to a normal passing test run.

## Restore and concurrency

Give restore an explicit owner for each distinct project graph, configuration,
RID, source policy and package cache. Reuse that restore when its inputs remain
valid. `--no-restore` is justified only after the needed assets exist for the
same graph; remove repeated restores after establishing graph coverage.

The current Testing entrypoint restores its solution and then probes separately,
although the tests already reference the probes. Investigate that overlap when
consolidating the graph. The build host also has its own restore/bootstrap needs.
Moving to another cache or changing sources/configuration can require a new
restore even when project text is unchanged.

Await dependent stages in order. Run independent work concurrently only when it
does not share mutable intermediate/output state. In particular, isolate RID
publish/restore graphs and output paths before introducing parallel work.
Do not tune CPU limits without inspecting process ownership and actual timings.

## Evidence and publication

Reuse passing checks while relevant inputs remain unchanged; broaden only for
an unresolved concern. Tests already compile changed code. Documentation-only
changes require content/link checks, not another execution gate. Keep required
test failures, unreadable reports and cleanup failures visible.

Keep candidate and public-package evidence separate. A successful source test
does not prove package layout or dependency metadata. Native AOT evidence, when
required for the supported scenario, means executing the actual published
artifact; successful managed execution or compilation alone is insufficient.

Use one `release.sh` lifecycle for the merged public library. Preserve exact
package/version identity, dependency order, failure propagation, indexing and
real public consumer verification. Retire the prototype's independent source
archive release after migration. Record committed, published, indexed and
verified states distinctly; do not call a candidate or upload alone a release
verification success.

Separate output directories for test layers, probes, package consumers and
release attempts. Aggregate valid coverage without treating blanket percentages
as the business contract. Scope JetBrains inspection to completed changes and
report tool failures separately from code findings.
