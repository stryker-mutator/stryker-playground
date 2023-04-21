using Microsoft.CodeAnalysis;
using Stryker.Core.Common.Mutants;
using Stryker.Core.Common.Options;
using System.Collections.Generic;

namespace Stryker.Core.Common.Mutators
{
    public interface IMutator
    {
        IEnumerable<Mutation> Mutate(SyntaxNode node, StrykerOptions options);
    }
}
