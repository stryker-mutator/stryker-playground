using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stryker.Core.Common.Mutants;
using Stryker.Core.Common.Mutators;

namespace Stryker.Playground.Domain.Compiling.Rollback;

public interface IRollbackProcess
{
    RollbackProcessResult Start(CSharpCompilation compiler, ImmutableArray<Diagnostic> diagnostics, bool lastAttempt, bool devMode);
}

/// <summary>
/// Responsible for rolling back all mutations that prevent compiling the mutated assembly
/// </summary>
public class RollbackProcess : IRollbackProcess
{
    private List<int> RolledBackIds { get; }
    private ILogger Logger { get; }

    public RollbackProcess()
    {
        Logger = NullLoggerFactory.Instance.CreateLogger<RollbackProcess>();
        RolledBackIds = new List<int>();
    }

    public RollbackProcessResult Start(CSharpCompilation compiler, ImmutableArray<Diagnostic> diagnostics, bool lastAttempt, bool devMode)
    {
        // match the diagnostics with their syntax trees
        var syntaxTreeMapping = compiler.SyntaxTrees.ToDictionary<SyntaxTree, SyntaxTree, ICollection<Diagnostic>>(syntaxTree => syntaxTree, _ => new Collection<Diagnostic>());

        foreach (var diagnostic in diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error))
        {
            syntaxTreeMapping[diagnostic.Location.SourceTree].Add(diagnostic);
        }

        // remove the broken mutations from the syntax trees
        foreach (var syntaxTreeMap in syntaxTreeMapping.Where(x => x.Value.Any()))
        {
            var originalTree = syntaxTreeMap.Key;
            if (devMode)
            {
                DumpBuildErrors(syntaxTreeMap);
            }
            else
            {
                Console.WriteLine($"RollBacking mutations from in-memory path");
            }

            Console.WriteLine($"source {originalTree}");
            var updatedSyntaxTree = RemoveMutantIfStatements(originalTree, syntaxTreeMap.Value, devMode);

            if (updatedSyntaxTree == originalTree && (lastAttempt || devMode))
            {
                Console.WriteLine(
                    "Stryker.NET could not compile the project after mutation. This is probably an error for Stryker.NET and not your project. Please report this issue on github with the previous error message.");
                throw new AggregateException("Internal error due to compile error.");
            }

            Console.WriteLine($"RolledBack to {updatedSyntaxTree}");

            // update the compiler object with the new syntax tree
            compiler = compiler.ReplaceSyntaxTree(originalTree, updatedSyntaxTree);
        }

        // by returning the same compiler object (with different syntax trees) the next compilation will use Roslyn's incremental compilation
        return new RollbackProcessResult()
        {
            Compilation = compiler,
            RollbackedIds = RolledBackIds
        };
    }

    // search is this node contains or is within a mutation
    private (SyntaxNode, int) FindMutationIfAndId(SyntaxNode startNode)
    {
        var info = ExtractMutationInfo(startNode);
        if (info.Id != null)
        {
            return (startNode, info.Id.Value);
        }
        for (var node = startNode; node != null; node = node.Parent)
        {
            info = ExtractMutationInfo(node);
            if (info.Id != null)
            {
                return (node, info.Id.Value);
            }
        }

        // scan within the expression
        return startNode is ExpressionSyntax ? FindMutationInChildren(startNode) : (null, -1);
    }

    // search the first mutation within the node
    private (SyntaxNode, int) FindMutationInChildren(SyntaxNode startNode)
    {
        foreach (var node in startNode.ChildNodes())
        {
            var info = ExtractMutationInfo(node);
            if (info.Id != null)
            {
                return (node, info.Id.Value);
            }
        }

        foreach (var node in startNode.ChildNodes())
        {
            var (subNode, mutantId) = FindMutationInChildren(node);
            if (subNode != null)
            {
                return (subNode, mutantId);
            }
        }

        return (null, -1);
    }

    private MutantInfo ExtractMutationInfo(SyntaxNode node)
    {
        var info = MutantPlacer.FindAnnotations(node);

        if (info.Engine == null)
        {
            return new MutantInfo();
        }

        Console.WriteLine($"Found mutant {info.Id} of type '{info.Type}' controlled by '{info.Engine}'.");

        return info;
    }

    private static SyntaxNode FindEnclosingMember(SyntaxNode node)
    {
        for (var currentNode = node; currentNode != null; currentNode = currentNode.Parent)
        {
            if (currentNode.IsKind(SyntaxKind.MethodDeclaration) || currentNode.IsKind(SyntaxKind.GetAccessorDeclaration) || currentNode.IsKind(SyntaxKind.SetAccessorDeclaration) || currentNode.IsKind(SyntaxKind.ConstructorDeclaration))
            {
                return currentNode;
            }
        }
        // return the all file if not found
        return node.SyntaxTree.GetRoot();
    }

    private void ScanAllMutationsIfsAndIds(SyntaxNode node, IList<MutantInfo> scan)
    {
        foreach (var childNode in node.ChildNodes())
        {
            ScanAllMutationsIfsAndIds(childNode, scan);
        }

        var info = ExtractMutationInfo(node);
        if (info.Id != null)
        {
            scan.Add(info);
        }
    }

    private void DumpBuildErrors(KeyValuePair<SyntaxTree, ICollection<Diagnostic>> syntaxTreeMap)
    {
        Console.WriteLine($"Roll backing mutations from {syntaxTreeMap.Key.FilePath}.");
        var sourceLines = syntaxTreeMap.Key.ToString().Split("\n");
        foreach (var diagnostic in syntaxTreeMap.Value)
        {
            var fileLinePositionSpan = diagnostic.Location.GetMappedLineSpan();
            Console.WriteLine($"Error :{diagnostic.GetMessage()}, {fileLinePositionSpan}");
            for (var i = Math.Max(0, fileLinePositionSpan.StartLinePosition.Line - 1);
                 i <= Math.Min(fileLinePositionSpan.EndLinePosition.Line + 1, sourceLines.Length - 1);
                 i++)
            {
                Console.WriteLine($"{i + 1}: {sourceLines[i]}");
            }
        }

        Console.WriteLine(Environment.NewLine);
    }

    private SyntaxTree RemoveMutantIfStatements(SyntaxTree originalTree, IEnumerable<Diagnostic> diagnosticInfo, bool devMode)
    {
        var rollbackRoot = originalTree.GetRoot();
        // find all if statements to remove
        var brokenMutations = new Collection<SyntaxNode>();
        var diagnostics = diagnosticInfo as Diagnostic[] ?? diagnosticInfo.ToArray();
        foreach (var diagnostic in diagnostics)
        {
            var brokenMutation = rollbackRoot.FindNode(diagnostic.Location.SourceSpan);
            var (mutationIf, mutantId) = FindMutationIfAndId(brokenMutation);
            if (mutationIf == null || brokenMutations.Contains(mutationIf))
            {
                continue;
            }

            brokenMutations.Add(mutationIf);
            if (mutantId >= 0)
            {
                RolledBackIds.Add(mutantId);
            }
        }

        if (brokenMutations.Count == 0)
        {
            // we were unable to identify any mutation that could have caused the build issue(s)
            foreach (var diagnostic in diagnostics)
            {
                var brokenMutation = rollbackRoot.FindNode(diagnostic.Location.SourceSpan);
                var errorLocation = diagnostic.Location.GetMappedLineSpan();
                Console.WriteLine(
                    $"Stryker.NET encountered an compile error in {errorLocation.Path} (at {errorLocation.StartLinePosition.Line}:{errorLocation.StartLinePosition.Character}) with message: {diagnostic.GetMessage()} (Source code: {brokenMutation})");

                if (devMode)
                {
                    Console.WriteLine("Stryker.NET will stop (due to dev-mode option sets to true)");
                    return originalTree;
                }

                var scan = new List<MutantInfo>();
                var initNode = FindEnclosingMember(brokenMutation);
                ScanAllMutationsIfsAndIds(initNode, scan);

                if (scan.Any(x => x.Type == Mutator.Block.ToString()))
                {
                    foreach (var mutant in scan.Where(x => x.Type == Mutator.Block.ToString() && !brokenMutations.Contains(x.Node)))
                    {
                        brokenMutations.Add(mutant.Node);
                        RolledBackIds.Add(mutant.Id.Value);
                    }
                }
                else
                {
                    Console.WriteLine(
                        "Safe Mode! Stryker will try to continue by rolling back all mutations in method. This should not happen, please report this as an issue on github with the previous error message.");
                    // backup, remove all mutations in the node

                    foreach (var mutant in scan.Where(mutant => !brokenMutations.Contains(mutant.Node)))
                    {
                        brokenMutations.Add(mutant.Node);
                        if (mutant.Id != -1)
                        {
                            RolledBackIds.Add(mutant.Id.Value);
                        }
                    }
                }
            }
        }

        // mark the broken mutation nodes to track
        var trackedTree = rollbackRoot.TrackNodes(brokenMutations);
        foreach (var brokenMutation in brokenMutations)
        {
            // find the mutated node in the new tree
            var nodeToRemove = trackedTree.GetCurrentNode(brokenMutation);
            // remove the mutated node using its MutantPlacer remove method and update the tree
            trackedTree = trackedTree.ReplaceNode(nodeToRemove, MutantPlacer.RemoveMutant(nodeToRemove));
        }
        return trackedTree.SyntaxTree;
    }
}