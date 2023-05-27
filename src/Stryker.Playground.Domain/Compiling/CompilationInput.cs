using Microsoft.CodeAnalysis;

namespace Stryker.Playground.Domain.Compiling;

public class CompilationInput
{
    public IEnumerable<MetadataReference> References { get; set; }
    
    public IEnumerable<string> UsingStatementNamespaces { get; set; }

    public SyntaxNode SourceCode { get; set; }

    public SyntaxNode TestCode { get; set; }

    public static string[] DefaultLibraries =
    {
        // System dependencies
        "System",
        "System.Console",
        "System.Collections",
        // "System.Core",
        "System.Runtime",
        // "System.IO",
        "System.Linq",
        "System.Linq.Expressions",
        // "System.Linq.Parallel",
        "System.Private.CoreLib",
        "netstandard",
        
        // 3rd party dependencies
        "Shouldly",
        "nunit.framework",
    };

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
}