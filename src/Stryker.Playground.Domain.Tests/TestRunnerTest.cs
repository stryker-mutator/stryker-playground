using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Stryker.Playground.Domain.Compiling;
using Stryker.Playground.Domain.TestRunners;

namespace Stryker.Playground.Domain.Tests;

public class TestRunnerTest
{
    private static string GenerateTestClass(int testCount, TestRunStatus status)
    {
        var sb = new StringBuilder(@"using NUnit.Framework;
namespace Tests 
{
    [TestFixture]
    public class DummyTests
    {");

        for (int i = 0; i < testCount; i++)
        {
            var expression = status switch
            {
                TestRunStatus.PASSED => "Assert.Pass();",
                TestRunStatus.FAILED => "Assert.Fail();",
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };
            
            var testCase = $@"[Test]
                            public void Dummy_Test_{i}()
                            {{
                                {expression}
                            }}";

            sb.Append(testCase);
        }

        sb.Append("}}");

        return sb.ToString();
    }
    
    private readonly PlaygroundCompiler _compiler = new();

    private async Task<CompilationResult> GetCompilation(string testCode)
    {
        var references = CompilationInput.DefaultLibraries
            .Select(x => MetadataReference.CreateFromFile(Assembly.Load(x).Location))
            .ToList();
        
        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        
        var input = new CompilationInput()
        {
            References = references,
            SourceCode = await SyntaxFactory.ParseSyntaxTree(string.Empty).GetRootAsync(),
            TestCode = await SyntaxFactory.ParseSyntaxTree(testCode).GetRootAsync(),
            UsingStatementNamespaces = CompilationInput.DefaultNamespaces,
        };

        return await _compiler.Compile(input);
    }
    
    public static IEnumerable<object[]> TestRunInputData =>
        new List<object[]>
        {
            new object[] { GenerateTestClass(1, TestRunStatus.PASSED), 1, TestRunStatus.PASSED },
            new object[] { GenerateTestClass(5, TestRunStatus.PASSED), 5, TestRunStatus.PASSED },
            
            new object[] { GenerateTestClass(1, TestRunStatus.FAILED), 1, TestRunStatus.FAILED },
            new object[] { GenerateTestClass(5, TestRunStatus.FAILED), 5, TestRunStatus.FAILED },
        };

    [Theory]
    [MemberData(nameof(TestRunInputData))]
    public async Task TestRunner_Reports_ValidTestResults(string testCode, int testCount, TestRunStatus status)
    {
        // Arrange
        var runner = new TestRunner();
        var compilation = await GetCompilation(testCode);
        
        Assert.True(compilation.Success);
        Assert.NotNull(compilation.EmittedBytes);

        // Act
        var results = await runner.RunTests(compilation.EmittedBytes);
        
        // Assert
        results.Status.ShouldBe(status);
        results.TestCount.ShouldBe(testCount);
        results.TextOutput.ShouldNotBeEmpty();
    }
    
    [Fact]
    public async Task TestRunner_Reports_MultipleFailedTests_When_StopOnErrorIsFalse()
    {
        // Arrange
        var runner = new TestRunner();
        var testCode = GenerateTestClass(5, TestRunStatus.FAILED);
        var compilation = await GetCompilation(testCode);
        
        Assert.True(compilation.Success);
        Assert.NotNull(compilation.EmittedBytes);

        // Act
        var results = await runner.RunTests(compilation.EmittedBytes, stopOnError: false);
        
        // Assert
        results.Status.ShouldBe(TestRunStatus.FAILED);
        results.TestCount.ShouldBe(5);
        results.FailedCount.ShouldBe(5);
        results.TextOutput.ShouldNotBeEmpty();
    }
    
    [Fact]
    public async Task TestRunner_Returns_OnFirstFailure_When_StopOnErrorIsTrue()
    {
        // Arrange
        var runner = new TestRunner();
        var testCode = GenerateTestClass(5, TestRunStatus.FAILED);
        var compilation = await GetCompilation(testCode);
        
        Assert.True(compilation.Success);
        Assert.NotNull(compilation.EmittedBytes);

        // Act
        var results = await runner.RunTests(compilation.EmittedBytes, stopOnError: true);
        
        // Assert
        results.Status.ShouldBe(TestRunStatus.FAILED);
        results.TestCount.ShouldBe(1); // Since the first test case failed and stopOnError = true, we only expect a count of 1
        results.FailedCount.ShouldBe(1);
        results.TextOutput.ShouldNotBeEmpty();
    }
}