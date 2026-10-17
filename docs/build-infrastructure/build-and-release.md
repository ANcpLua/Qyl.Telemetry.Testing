# Build and package mechanics

## Current entrypoints

The starting Testing checkout uses `build.sh`,
`eng/build/_build.csproj` and `eng/build/Build.cs`. The prototype has a separate
`build/` host and `build.sh`. Both use Fallout, an improved NUKE fork. Fallout's
local implementation is available at
`/Users/alexandernachtmann/RiderProjects/Fallout`.

The existing Testing entrypoint restores its solution and probes separately;
the tests also reference the probes. That relationship describes overlapping
restore graphs, while the build host has additional bootstrap dependencies.

## Restore assets and execution

Restore depends on project/import inputs, framework, configuration, RID, package
sources and cache locations. `--no-restore` uses existing restore assets;
`--no-build` uses existing compiled outputs. Neither flag establishes that the
artifacts match the current inputs.

Project references establish build dependencies. Unrelated stages can overlap,
while stages consuming an output depend on its completion. Shared mutable
intermediates can create races even when processes have different final output
folders.

## Test and consumer boundaries

| Boundary | What executes |
| --- | --- |
| Pure logic | In-memory assertion, filtering or evidence processing |
| Component interaction | Receivers, transports, exporters and lifecycle behavior |
| Assembled application | Applications and their external components |
| Candidate package consumer | Outputs and dependencies from a locally packed package |
| Public package consumer | Outputs and dependencies restored from the public package |

A test filename does not establish its boundary. A model-named test can still
start an HTTP receiver. Deliberate failure probes produce expected failed child
results; those results differ from the outcome of their parent verification
suite.

MSTest discovery, execution and structured reporting expose different evidence.
Source generation, managed execution and execution of a Native AOT artifact
also represent distinct mechanisms.

## Package and release artifacts

Packing produces a package containing assemblies, dependency metadata and other
payload. Package verification can observe contents and restore behavior that a
source build does not exercise. The current library also ships proto licensing
and notices through package items.

Publication uploads an artifact. Indexing makes its version discoverable through
registry metadata. Consumer execution observes the restored artifact's behavior.
A release can therefore have different publication, indexing and consumer
verification states.

The Testing release tooling packs a candidate, exercises a consumer, uploads the
package and performs public-package verification. The prototype release tooling
publishes a checked source archive. These are different existing release models,
not two names for the same operation.
