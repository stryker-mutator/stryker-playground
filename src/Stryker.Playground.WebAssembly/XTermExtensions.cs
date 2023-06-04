using Stryker.Playground.Domain.TestRunners;
using XtermBlazor;
using static Crayon.Output;

namespace Stryker.Playground.WebAssembly;

public static class XTermExtensions
{
    public static async Task Error(this Xterm xterm, string message)
    {
        await xterm.WriteAndScroll(Red(Bold(message)));
    }
    
    public static async Task Warning(this Xterm xterm, string message)
    {
        await xterm.WriteAndScroll(Yellow(Bold(message)));
    }
    
    public static async Task Success(this Xterm xterm, string message)
    {
        await xterm.WriteAndScroll(Green(Bold(message)));
    }
    
    public static async Task WriteAndScroll(this Xterm xterm, string message)
    {
        await xterm.WriteLine(message);
        await xterm.ScrollToBottom();
        await Task.Delay(5);
    }

    public static async Task DisplayMutationScore(this Xterm xterm, double mutationScore)
    {
        var messageTxt = $"Your mutation score is {mutationScore:N2}%";

        var msg = mutationScore switch
        {
            >= 80 => Green(Bold(messageTxt)),
            >= 60 => Yellow(Bold(messageTxt)),
            _ => Red(Bold(messageTxt))
        };

        await xterm.WriteAndScroll(msg);
    }

    public static string GetResultMessage(this TestRunResult result)
    {
        if (result.Status == TestRunStatus.PASSED)
        {
            return Green(Bold($"All {result.TestCount} tests passed"));
        }

        if (result.Status == TestRunStatus.TIMEOUT)
        {
            return Red(Bold("Test suite exceeded timeout."));
        }

        if (result.TestCount == 0)
        {
            return Yellow(Bold("Test suite does not contain any tests."));
        }
        
        if (result.FailedCount > 0)
        {
            return Red(Bold($"{result.FailedCount} test(s) failed."));
        }

        return Yellow(Bold("An unexpected error occurred. Please contact the maintainers if you see this message."));
    }
}