using Microsoft.CodeAnalysis;

namespace Stryker.Playground.Domain.Compiling;

public class CompilationInput
{
    public IEnumerable<MetadataReference> References { get; set; }

    public SyntaxNode SourceCode { get; set; }

    public SyntaxNode TestCode { get; set; }

    public string[] GlobalUsingDirectives { get; set; } = DefaultGlobalUsings;

    public static readonly string[] DefaultLibraries =
    {
        // System dependencies
        "System",
        "System.Console",
        "System.Collections",
        "System.Runtime",
        "System.Linq",
        "System.Linq.Expressions",
        "System.Private.CoreLib",
        "netstandard",
        
        // 3rd party dependencies
        "Shouldly",
        "nunit.framework",
    };

    private static readonly string[] DefaultGlobalUsings =
    {
        "Playground.Source", 
        "NUnit.Framework", 
        "Shouldly",
        "System",
        "System.IO",
        "System.Linq",
        "System.Collections.Generic",
        "System.Threading",
        "System.Threading.Tasks",
    };
}