using System.Text.Json;
using DotCov;
using DotCov.Formatters;
using Serilog;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;
using Fallout.Solutions;
using JetBrains.Annotations;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

namespace _build;

[UsedImplicitly]
internal sealed class Build : FalloutBuild
{
    public static int Main() => Execute<Build>(x => x.Check);
    [Solution("Telemetry.Testing.slnx", GenerateProjects = false)]
    private readonly Solution _solution = null!;
    private static AbsolutePath Tests => RootDirectory / "tests/Qyl.Telemetry.Testing.Tests/Qyl.Telemetry.Testing.Tests.csproj";
    private static AbsolutePath CoverageDirectory => RootDirectory / "artifacts/coverage";

    [UsedImplicitly]
    private Target Compile => target => target.Executes(() => DotNetBuild(s => s.SetProjectFile(_solution)
        .SetConfiguration("Release").EnableNoRestore().SetProcessAdditionalArguments("-m:4")));

    private Target Coverage => target => target.DependsOn(Compile).Executes(() =>
    {
        CoverageDirectory.CreateOrCleanDirectory();
        DotNet($"test --project \"{Tests}\" -c Release --no-build --no-restore --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml --report-trx --results-directory \"{CoverageDirectory}\"");
        var inputs = ReportResolver.Resolve(CoverageDirectory);
        Log.Information("Coverage inputs: {Inputs}", inputs.Select(input => input.SourceName));
        var report = CoberturaParser.Parse(inputs);
        if (report.Files.Count == 0) throw new InvalidOperationException("The test run produced no coverage measurements.");
        foreach (var warning in report.Warnings)
            Log.Warning("Coverage: {Warning}", warning.Detail);
        Log.Information("Unfiltered coverage (informational):\n{Report}", TableFormatter.Format(report));
        File.WriteAllText(CoverageDirectory / "coverage.json", JsonFormatter.Format(report));
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
