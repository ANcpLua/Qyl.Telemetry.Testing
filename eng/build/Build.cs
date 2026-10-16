using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using DotCov.Fallout;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;
using JetBrains.Annotations;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

namespace _build;

[UsedImplicitly]
internal sealed class Build : FalloutBuild, ICoverageReport
{
    public static int Main() => Execute<Build>(x => x.Check);
    private static AbsolutePath Solution => RootDirectory / "Telemetry.Testing.slnx";
    private static AbsolutePath Tests => RootDirectory / "tests/Qyl.Telemetry.Testing.Tests/Qyl.Telemetry.Testing.Tests.csproj";
    private static AbsolutePath CoverageDirectory => RootDirectory / "artifacts/coverage";
    string ICoverageReport.MinLine => "100";
    string ICoverageReport.MinBranch => "100";
    AbsolutePath ICoverageReport.CoverageSearchDirectory => CoverageDirectory;

    [UsedImplicitly]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Fallout discovers instance Target properties; a static target cannot be invoked by its CLI.")]
    private Target Compile => target => target.Executes(() => DotNetBuild(s => s.SetProjectFile(Solution)
        .SetConfiguration("Release").EnableNoRestore().SetProcessAdditionalArguments("-m:4")));

    private Target Coverage => target => target.DependsOn(Compile).Triggers<ICoverageReport>(x => x.ReportCoverage).Executes(() =>
    {
        CoverageDirectory.CreateOrCleanDirectory();
        DotNet($"test --project \"{Tests}\" -c Release --no-build --no-restore --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml --report-trx --results-directory \"{CoverageDirectory}\"");
    });

    private Target InspectCode => target => target.DependsOn(Compile).Executes(() =>
    {
        var report = RootDirectory / "artifacts/inspection.sarif";
        DotNet($"tool run jb -- inspectcode \"{RootDirectory / "eng/inspection.slnx"}\" --no-build --properties=Configuration=Release --severity=INFO --format=Sarif --output=\"{report}\"");
        using var document = JsonDocument.Parse(File.ReadAllText(report));
        var count = document.RootElement.GetProperty("runs").EnumerateArray()
            .Sum(run => run.TryGetProperty("results", out var results) ? results.GetArrayLength() : 0);
        if (count != 0) throw new InvalidOperationException($"InspectCode reported {count} findings: {report}");
    });

    private Target Check => target => target.DependsOn(Coverage, InspectCode);
    [UsedImplicitly]
    private Target Pack => target => target.DependsOn(Check).Executes(() => DotNetPack(s => s
        .SetProject(RootDirectory / "src/Qyl.Telemetry.Testing/Qyl.Telemetry.Testing.csproj")
        .SetConfiguration("Release").EnableNoBuild().EnableNoRestore().SetOutputDirectory(RootDirectory / "artifacts/packages-release")));
}
