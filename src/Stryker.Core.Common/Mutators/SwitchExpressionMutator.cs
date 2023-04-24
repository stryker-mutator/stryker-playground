using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Core.Common.Mutants;

namespace Stryker.Core.Common.Mutators
{
    /// <summary> Mutator implementation for switch expression</summary>
    public class SwitchExpressionMutator : PatternMutatorBase<SwitchExpressionSyntax>
    {
        /// <summary> Apply mutations to all <see cref="PatternSyntax"/> inside an <see cref="SwitchExpressionSyntax"/></summary>
        public override IEnumerable<Mutation> ApplyMutations(SwitchExpressionSyntax node) => node
            .DescendantNodes()
            .OfType<PatternSyntax>()
            .SelectMany(x => ApplyMutations(x));
    }
}
