﻿using BlazorMonaco.Editor;
using XtermBlazor;
using static Crayon.Output;

namespace Stryker.Playground.WebAssembly;

public class PlaygroundConstants
{
    public static string[] WelcomeMessageLines = 
    {
        "This is an interactive demo of Stryker.NET, the mutation testing framework for .NET!",
        "Some technical limitations apply as this runs entirely in your browser using WebAssembly.",
        $"For the full experience, install Stryker using {Bold("dotnet tool install -g dotnet-stryker")}",
        "",
        "Mutation testing is a technique for identifying weaknesses in your test suite by injecting bugs (mutations)",
        "into your source code and seeing if your tests can catch them.",
        "The result is a mutation score, which is the percentage of mutations that are 'killed' by your tests.",
        "This goes beyond code coverage, because it evaluates how effective your tests are at detecting faults in your code.",
        "",
        $"To get started, run the mutation tests by clicking on '{Yellow(Bold("Run Stryker"))}' in the top right corner",
        "Once executed, explore the generated mutation report to inspect the different types of mutations introduced.",
        "",
        "Try improving the mutation score by adding or editing test cases!",
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
        Rows = 14,
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
    
    [TestCase(1, 1, 1)]
    [TestCase(2, 1, 2)]
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