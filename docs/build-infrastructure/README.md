# Local build and package guidance

Adapted local references for preparing the telemetry-testing monorepo. Read this
index, then only the topic needed. These files summarize relevant dotnet/skills
guidance in our own words and record repository-specific decisions; they are not
installed skills or additional agent instructions. Normal use needs no web fetch.
Exact upstream reference copies are retained under `upstream/` for an unresolved
syntax detail. They are optional: do not load the whole directory or adopt their
generic workflows as repository policy. The short adapted topics are the normal
reading path.

| Task | Read |
| --- | --- |
| Shared defaults, imports, outputs, generated files, custom targets | [MSBuild](msbuild.md) |
| Central package versions, SDK pins, collector assets, package sources | [NuGet and CPM](nuget-cpm.md) |
| Restore ownership, test layers, candidate packages, publication | [Build and release](build-and-release.md) |
| Verify upstream provenance or investigate a later API change | [Source inventory](sources.json) |

The proposed main projects are `Qyl.Telemetry.Testing`,
`Qyl.Telemetry.Testing.UnitTests`, `Qyl.Telemetry.Testing.IntegrationTests` and
`Qyl.Telemetry.Testing.EndToEndTests`. Supporting applications and deliberate
failure probes may remain separate executables. This layout is a design target;
these documentation files do not implement the migration.

The starting checkout has `Telemetry.Testing.slnx`, one library, a combined tests
project, a child probe executable and `eng/build/_build.csproj`. Package versions
are inline; `Directory.Packages.props` does not yet exist. Recheck relevant files
as the template changes instead of treating this description as permanent state.

Excluded from the adaptation: generic agent routing, instructions to delegate,
legacy project migrations, unrelated frameworks, automatic package upgrades,
mandatory whole-suite clean/build cycles and speculative performance tuning.
Keep technical constraints and failure propagation when simplifying workflows.

Fallout is an improved NUKE fork. Resolve Fallout questions from the local
`/Users/alexandernachtmann/RiderProjects/Fallout` source; do not fetch web guidance
for Fallout or assume upstream NUKE behavior is identical.
