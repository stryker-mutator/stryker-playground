namespace Stryker.Playground.Domain.TestRunners;

public class TestRunResult
{
    public TestRunStatus Status { get; set; }
    
    public int TestCount { get; set; }
    public int FailedCount { get; set; }

    public int[] CoveredMutantIds { get; set; }

    public IEnumerable<string> TextOutput { get; set; }
}

public enum TestRunStatus
{
    PASSED,
    FAILED,
    TIMEOUT
}