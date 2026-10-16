using JetBrains.Annotations;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace Qyl.Telemetry.Testing.Tests;

[TestClass]
public static class TelemetryLifecycle
{
    internal static readonly HttpClient Http = new();

    [AssemblyCleanup]
    public static void CloseHttpClient()
    {
        Http.Dispose();
    }

    [UsedImplicitly]
    [GlobalTestInitialize]
    public static void Initialize(TestContext context)
    {
        QylTestHooks.Initialize(context);
    }

    [UsedImplicitly]
    [GlobalTestCleanup]
    public static void Cleanup(TestContext context)
    {
        QylTestHooks.Cleanup(context);
    }
}