using BlazorMonaco.Editor;
using XtermBlazor;

namespace Stryker.Playground.WebAssembly;

public class PlaygroundConstants
{
    public static string[] WelcomeMessageLines = 
    {
        "Welcome to the Stryker Playground!",
        "This is an interactive demo of Stryker.NET, the mutation testing framework for .NET.",
        "",
        "Mutation testing is a technique for identifying weaknesses in your test suite by injecting bugs (mutations)",
        "into your source code and seeing if your tests can catch them.",
        "This results in a mutation score, which is the percentage of mutations that are killed by your tests.",
        "Code coverage only tells you how much of your code is executed by your tests, whereas mutation score measures how well",
        "your tests are actually detecting faults in your code.",
        "",
        "To get started, run the mutation tests by clicking 'Run Mutation Tests'. This will show your initial mutation score.",
        "Edit your tests (and source, if you want) and then rerun the mutation tests to see how your score improves."
    };

    
    public static StandaloneEditorConstructionOptions EditorOptions(StandaloneCodeEditor editor)
    {
        return new StandaloneEditorConstructionOptions
        {
            AutomaticLayout = false,
            Language = "csharp",
            Theme = "vs-dark",
            Value = editor.Id.StartsWith("test") 
                ? UnitTestClassExample 
                : SourceCodeExample,
            Minimap = new EditorMinimapOptions
            {
                Enabled = false,
            },
            SmoothScrolling = true,
        };
    }
    
    public static TerminalOptions XTermOptions = new()
    {
        CursorBlink = true,
        CursorStyle = CursorStyle.Block,
        Theme =
        {
            Background = "#000000",
        },
        Columns = 160,
        Rows = 12,
    };
    
    public static TimeSpan TestSuiteMaxDuration = TimeSpan.FromSeconds(5);
    
    public static string UnitTestClassExample = @"namespace Playground.Tests;

public class CalculatorTests
{
    private readonly Calculator _calculator = new();

    [TestCase(100, 0, 100)]
    public void Addition_Returns_ExpectedResult(int a, int b, int expectedResult)
    {
        var result = _calculator.Add(a, b);

        result.ShouldBe(expectedResult);
    }
    
    [TestCase(30, 0, 30)]
    public void Subtraction_Returns_ExpectedResult(int a, int b, int expectedResult)
    {
        var result = _calculator.Subtract(a, b);

        result.ShouldBe(expectedResult);
    }
    
    [TestCase(0, 1, 0)]
    [TestCase(1, 1, 1)]
    public void Multiply_Returns_ExpectedResult(int a, int b, int expectedResult)
    {
        var result = _calculator.Multiply(a, b);

        result.ShouldBe(expectedResult);
    }
}";
    
    public static string SourceCodeExample = @"namespace Playground.Source;

public class Calculator
{
    public int Add(int a, int b)
    {
        return a + b;
    }
    
    public int Subtract(int a, int b)
    {
        return a - b;
    }
    
    public int Multiply(int a, int b)
    {
        return a * b;
    }
}";

}