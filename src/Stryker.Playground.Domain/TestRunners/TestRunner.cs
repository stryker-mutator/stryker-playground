using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using NUnit.Common;

namespace Stryker.Playground.Domain.TestRunners;

public class TestRunner : ITestRunner
{
    private static readonly ImmutableList<string> DefaultNUnitArguments = ImmutableList.Create<string>().Add("--noresult").Add("--labels=ON");
    
    public async Task<TestRunResult> RunTests(byte[] assemblyBytes, int? activeMutantId = null, bool stopOnError = false)
    {
        // This method may run in a Web Worker and must be declared as async
        // despite the fact that it contains no asynchronous code
        await Task.CompletedTask;
        
        activeMutantId ??= -1;
        Environment.SetEnvironmentVariable("ActiveMutation", activeMutantId.ToString());
        Environment.SetEnvironmentVariable("CoveredMutations", string.Empty);
        
        var arguments = stopOnError ? DefaultNUnitArguments.Add("--stoponerror") : DefaultNUnitArguments;
        var assembly = Assembly.Load(assemblyBytes);
        var sw = new StringWriter();
        var writer = new ExtendedTextWrapper(sw);
        var listener = new NUnitTestListener(assembly);
        
        listener.Execute(writer, TextReader.Null, arguments.ToArray());

        var coveredMutations = (Environment.GetEnvironmentVariable("CoveredMutations") ?? string.Empty).Split(",")
            .Where(x => !string.IsNullOrEmpty(x))
            .Select(int.Parse);

        return new TestRunResult()
        {
            Status = listener.Summary.FailedCount == 0 && listener.Summary.TestCount > 0 ? TestRunStatus.PASSED : TestRunStatus.FAILED,
            FailedCount = listener.Summary.FailedCount,
            TestCount = listener.Summary.TestCount,
            TextOutput = sw.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
            CoveredMutantIds = coveredMutations.ToArray(),
        };
    }
}