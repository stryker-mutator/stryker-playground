using Microsoft.CodeAnalysis.CSharp;

namespace Stryker.Playground.Domain.Compiling.Rollback
{

    /// <summary>
    /// Responsible for rolling back all mutations that prevent compiling the mutated assembly
    /// </summary>
    public class RollbackProcessResult
    {
        public CSharpCompilation Compilation { get; set; }
        public IEnumerable<int> RollbackedIds { get; set; } = Enumerable.Empty<int>();
    }
}