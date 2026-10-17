# MSBuild organization

## Place policy at the right scope

`Microsoft.Common.props` searches upward from each project and imports the
nearest `Directory.Build.props` early. Put shared defaults there; the project
can subsequently override them. `Microsoft.Common.targets` imports the nearest
`Directory.Build.targets` later. Neither hierarchy automatically merges every
ancestor: an inner file must explicitly import its intended parent.

Consolidate nullable, implicit usings, warnings, analysis and repository metadata
without losing exceptions needed by the build host or fixtures. Keep package
versions in the CPM policy, not duplicated in compiler defaults. Keep unique
framework references, protobuf items and dependencies in their owning project.
Default `IsPackable` to `false` and enable it explicitly for the public library.

Create `Directory.Build.targets` only for actual targets or late-bound logic.
Do not add an empty hierarchy or copy the Collector's entire build pipeline.
Evaluate conditions only after the properties they need are available. In
particular, a project-body `TargetFramework` may be empty in an early import;
item conditions outside targets also evaluate during project evaluation.

## Preserve resource and output identity

Keep intermediate and output files distinct across projects, configurations,
frameworks and RIDs. If adopting centralized SDK artifact output, verify both
the evaluated paths and every consumer of those paths. Do not assume setting
an artifact-root property alone replaces an existing explicit path layout.

Known migration touchpoints:

- `NativeRunnerTests.cs` currently locates its probe under a constructed
  `bin/<configuration>/net10.0` path. Update that lookup if outputs move; prefer
  evaluated project output metadata over a second manually maintained layout.
- The prototype's `Architecture.Testing.csproj` copies collector assets from a
  version/RID-specific NuGet path. Keep that acquisition with the fixture that
  owns the Collector and verify its package path under the chosen cache layout.
- Keep each row's workspace, process and evidence files separate. Shared
  immutable publication does not justify shared mutable child execution.

## Generated files and target behavior

Evaluation-time globs cannot discover files first created later during target
execution. A custom generator must register its outputs with the appropriate
items and `FileWrites`, using the evaluated intermediate directory. Choose the
hook before the actual consuming target; use declared dependencies rather than
assuming project-file order controls execution.

Preserve the existing `Protobuf` items and `Grpc.Tools` integration. Roslyn
generator outputs and SDK-managed generated files do not need another custom
`Compile` glob or a duplicate generation target.

For custom incremental targets, identify real inputs and outputs. Ensure item
registration still happens when generation is skipped as up-to-date. Do not
delete outputs on every build to disguise incorrect dependency tracking. Keep
expensive I/O and process execution out of ordinary project evaluation.

## Verify the changed graph

Use the actual affected project and configuration. A focused test invocation
already compiles its changed dependency graph. Inspect evaluated properties or a
preprocessed project for import uncertainty; use a binary log for an unresolved
execution problem. Keep logs out of committed documentation and avoid printing
credentials or sensitive build properties. Performance claims require measured
evidence, not a larger concurrency number or a target's inclusive wait time.
