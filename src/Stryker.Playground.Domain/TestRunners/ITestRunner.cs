namespace Stryker.Playground.Domain.TestRunners;

public interface ITestRunner
{
    public Task<TestRunResult> RunTests(byte[] assemblyBytes, int? activeMutantId = null);
}