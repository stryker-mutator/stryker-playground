using System.Reflection;
using NUnit.Common;

namespace Stryker.Playground.Domain.TestRunners;

public class TestRunner : ITestRunner
{
    private static readonly string[] NUnitArguments = { "--noresult", "--labels=ON" };
    
    public async Task<TestRunResult> RunTests(byte[] assemblyBytes, int? activeMutantId = null)
    {
        // This method may run in a Web Worker and must be declared as async
        // despite the fact that is contains no asynchronous code
        await Task.CompletedTask;
        activeMutantId ??= -1;
        
        Environment.SetEnvironmentVariable("ActiveMutation", activeMutantId.ToString());

        var assembly = Assembly.Load(assemblyBytes);
        var sw = new StringWriter();
        
        // var writer = new ExtendedTextWrapper(Console.Out);
        var writer = new ExtendedTextWrapper(sw);

        var listener = new NUnitTestListener(assembly);
        
        listener.Execute(writer, TextReader.Null, NUnitArguments);
        
        return new TestRunResult()
        {
            FailedCount = listener.Summary.FailedCount,
            TestCount = listener.Summary.TestCount,
            TextOutput = sw.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
        };
    }
}