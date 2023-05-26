using Microsoft.CodeAnalysis;
using Stryker.Core.Common.Mutants;

namespace Stryker.Playground.Domain.Compiling;

public class MutantCompilationResult : CompilationResult
{
    public IEnumerable<Mutant> Mutants { get; set; }
    public SyntaxNode OriginalTree { get; set; }
}