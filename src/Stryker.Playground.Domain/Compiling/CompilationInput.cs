using Microsoft.CodeAnalysis;

namespace Stryker.Playground.Domain.Compiling;

public class CompilationInput
{
    public IEnumerable<MetadataReference> References { get; set; }
    
    public IEnumerable<string> UsingStatementNamespaces { get; set; }

    public string SourceCode { get; set; }

    public string TestCode { get; set; }
}