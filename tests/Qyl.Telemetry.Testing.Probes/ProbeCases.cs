using System.Diagnostics;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace Qyl.Telemetry.Testing.Probes;

[QylTelemetry]
public sealed class ProbeCases
{
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    public void RowsKeepTheirArgumentsAndRoot(int value)
    {
        Assert.IsTrue(value is 1 or 2);
        Assert.AreEqual("qyl.test", Activity.Current?.OperationName);
        Assert.AreEqual(Activity.Current?.TraceId.ToHexString(), TestContext.Properties[QylTestHooks.TraceIdProperty]);
        TestContext.WriteLine($"ROW:{value}:TRACE:{Activity.Current?.TraceId.ToHexString()}");
    }

    [TestMethod]
    public void OriginalAssertionFailure() => Assert.Fail("original assertion survives");

    [TestMethod]
    public void OriginalExceptionFailure() => throw new InvalidOperationException("original exception survives");

    [JetBrains.Annotations.UsedImplicitly]
    public TestContext TestContext { get; set; } = null!;
}

[TestClass]
public static class ProbeLifecycle
{
    [JetBrains.Annotations.UsedImplicitly]
    [GlobalTestInitialize]
    public static void Initialize(TestContext context) => QylTestHooks.Initialize(context);
    [JetBrains.Annotations.UsedImplicitly]
    [GlobalTestCleanup]
    public static void Cleanup(TestContext context) => QylTestHooks.Cleanup(context);
}
