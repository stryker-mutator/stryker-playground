namespace Stryker.Playground.Domain.TestRunners;

public class TestRunResult
{
    public int TestCount { get; set; }
    public int FailedCount { get; set; }

    public IEnumerable<string> TextOutput { get; set; }
}