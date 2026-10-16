using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace Qyl.Telemetry.Testing.Tests;

[TestClass]
public sealed class NativeRunnerTests
{
    private static string _probe = null!;
    private static string _source = null!;

    [UsedImplicitly] public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void LocateProbe(TestContext _)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Telemetry.Testing.slnx"))) root = root.Parent;
        Assert.IsNotNull(root);
        var project = Path.Combine(root.FullName,
            "tests/Qyl.Telemetry.Testing.Probes/Qyl.Telemetry.Testing.Probes.csproj");
#if DEBUG
        const string configuration = "Debug";
#else
        const string configuration = "Release";
#endif
        _probe = Path.Combine(Path.GetDirectoryName(project)!, "bin", configuration,
            "net10.0/Qyl.Telemetry.Testing.Probes.dll");
        _source = Path.Combine(Path.GetDirectoryName(project)!, "ProbeCases.cs");
        Assert.IsTrue(File.Exists(_probe), $"The declared probe project must be built before execution: {_probe}");
    }

    [TestMethod]
    public async Task Native_results_preserve_both_failures_and_each_data_row_identity()
    {
        var directory = Directory.CreateTempSubdirectory("qyl-native-probe-");
        try
        {
            var run = await RunAsync(TestContext.CancellationToken, _probe, "--report-trx", "--results-directory",
                directory.FullName);
            Assert.AreEqual(2, run.ExitCode);
            var report = XDocument.Load(Assert.ContainsSingle(Directory.GetFiles(directory.FullName, "*.trx")));
            XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
            var results = report.Descendants(ns + "UnitTestResult").ToArray();
            Assert.HasCount(4, results);
            Assert.HasCount(2, results.Where(r => (string?)r.Attribute("outcome") == "Passed"));
            Assert.HasCount(2, results.Where(r => (string?)r.Attribute("outcome") == "Failed"));
            Assert.HasCount(4, results.Select(r => (string?)r.Attribute("testId")).Distinct());
            var assertion = results.Single(r =>
                ((string?)r.Attribute("testName"))!.Contains("OriginalAssertionFailure", StringComparison.Ordinal));
            var exception = results.Single(r =>
                ((string?)r.Attribute("testName"))!.Contains("OriginalExceptionFailure", StringComparison.Ordinal));
            Assert.AreEqual("Assertion failed.\noriginal assertion survives",
                assertion.Descendants(ns + "Message").Single().Value);
            Assert.Contains("ProbeCases.OriginalAssertionFailure()",
                assertion.Descendants(ns + "StackTrace").Single().Value);
            Assert.Contains("original exception survives", exception.Value);
            Assert.Contains("InvalidOperationException", exception.Value);
            var rows = results.Where(r => (string?)r.Attribute("outcome") == "Passed").ToArray();
            Assert.Contains("ROW:1:TRACE:",
                rows.Single(r => ((string?)r.Attribute("testName"))!.Contains("(1)", StringComparison.Ordinal)).Value);
            Assert.Contains("ROW:2:TRACE:",
                rows.Single(r => ((string?)r.Attribute("testName"))!.Contains("(2)", StringComparison.Ordinal)).Value);
            foreach (var result in results) Assert.IsNotNull(result.Attribute("duration"));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [TestMethod]
    public async Task Discovery_points_to_the_original_native_attribute_for_every_wrapped_row()
    {
        var run = await RunAsync(TestContext.CancellationToken, _probe, "--list-tests", "json");
        Assert.AreEqual(0, run.ExitCode, run.Error);
        using var report = JsonDocument.Parse(run.Output);
        var rows = report.RootElement.GetProperty("tests").EnumerateArray().Where(t =>
            t.GetProperty("type").GetProperty("methodName").GetString() == "RowsKeepTheirArgumentsAndRoot").ToArray();
        Assert.HasCount(2, rows);
        var source = await File.ReadAllLinesAsync(_source, TestContext.CancellationToken);
        var expectedLine = Array.FindIndex(source,
            line => line.Contains("public void RowsKeepTheirArgumentsAndRoot", StringComparison.Ordinal)) - 2;
        foreach (var row in rows)
        {
            var location = row.GetProperty("location");
            Assert.AreEqual(_source, location.GetProperty("file").GetString());
            Assert.AreEqual(expectedLine, location.GetProperty("lineStart").GetInt32());
            Assert.AreEqual(expectedLine, location.GetProperty("lineEnd").GetInt32());
        }
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunAsync(CancellationToken cancellationToken,
        params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet")
            { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = new Process();
        process.StartInfo = start;
        Assert.IsTrue(process.Start());
        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            await process.WaitForExitAsync(cancellationToken);
            return (process.ExitCode, await output, await error);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(true);
                await process.WaitForExitAsync(CancellationToken.None);
            }
        }
    }
}