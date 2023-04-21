using BlazorMonaco.Editor;
using XtermBlazor;

namespace Stryker.Playground.WebAssembly;

public class PlaygroundConstants
{
    public static StandaloneEditorConstructionOptions EditorOptions(StandaloneCodeEditor editor)
    {
        return new StandaloneEditorConstructionOptions
        {
            AutomaticLayout = true,
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
    
    public static string UnitTestClassExample = @"using Playground.Example.Source;
using NUnit.Framework;

namespace Playground.Example.Tests 
{
    [TestFixture]
    public class RoboBarTests
    {
        [Test]
        public void RoboBar_OffersBeer_When_CustomerIs18Plus()
        {
            var bar = new RoboBar();
            var response = bar.GetGreeting(19);

            Assert.That(response, Is.EqualTo(""Here have a beer!""));
        }

        [Test]
        public void RoboBar_DeniesService_When_CustomerIsUnderage()
        {
            var bar = new RoboBar();
            var response = bar.GetGreeting(17);

            Assert.That(response, Is.EqualTo(""Sorry not today!""));
        }
    }
}";
    
    public static string SourceCodeExample = @"using System;

namespace Playground.Example.Source 
{
    public class RoboBar
    {
        public string GetGreeting(int age)
        {
            if(age >= 18)
            {
                return ""Here have a beer!"";
            }

            return ""Sorry not today!"";
        }
    }
}";

    public static string[] DefaultNamespaces =
    {
        "System", 
        "System.Text", 
        "System.Collections.Generic", 
        "System.IO", 
        "System.Linq",
        "System.Console",
        "System.Threading", 
        "System.Threading.Tasks"
    };
    
    public static string[] DefaultLibraries =
    {
        // System dependencies
        "System.dll",
        "System.Console.dll",
        "System.Collections.dll",
        "System.Core.dll",
        "System.Runtime.dll",
        "System.IO.dll",
        "System.Linq.dll",
        "System.Linq.Expressions.dll",
        "System.Linq.Parallel.dll",
        "System.Private.CoreLib.dll",
        "netstandard.dll",
        
        // 3rd party dependencies
        "nunit.framework.dll",
    };

}