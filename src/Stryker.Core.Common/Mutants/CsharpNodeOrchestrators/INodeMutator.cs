using Microsoft.CodeAnalysis;
using Stryker.Core.Common.Helpers;

namespace Stryker.Core.Common.Mutants.CsharpNodeOrchestrators
{
    internal interface INodeMutator : ITypeHandler<SyntaxNode>
    {
        SyntaxNode Mutate(SyntaxNode node, MutationContext context);
    }
}
