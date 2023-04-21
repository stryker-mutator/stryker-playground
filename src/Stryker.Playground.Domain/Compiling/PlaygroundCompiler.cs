using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;
using Stryker.Core.Common.InjectedHelpers;
using Stryker.Core.Common.Logging;
using Stryker.Core.Common.Mutants;

namespace Stryker.Playground.Domain.Compiling;

public class PlaygroundCompiler : IPlaygroundCompiler
{
    public PlaygroundCompiler()
    {
        ApplicationLogging.LoggerFactory = NullLoggerFactory.Instance;
    }
    
    public async Task<MutantCompilationResult> CompileWithMutations(CompilationInput input)
    {
        var orchestrator = new CsharpMutantOrchestrator();

        var sourceCodeTree = SyntaxFactory.ParseSyntaxTree(input.SourceCode.Trim());
        var sourceCodeRoot = await sourceCodeTree.GetRootAsync();

        Console.WriteLine("Original syntax tree:");
        Console.WriteLine(sourceCodeRoot.ToFullString());

        var mutatedTree = orchestrator.Mutate(sourceCodeRoot);

        Console.WriteLine($"Mutated the syntax tree with {orchestrator.MutantCount} mutations:");
        Console.WriteLine(mutatedTree.ToFullString());

        input.SourceCode = mutatedTree.ToString();

        var compilationResult = await Compile(input);

        return new MutantCompilationResult
        {
            Mutants = orchestrator.Mutants,
            Diagnostics = compilationResult.Diagnostics,
            EmittedBytes = compilationResult.EmittedBytes,
            Success = compilationResult.Success,
        };
    }
    
    public async Task<CompilationResult> Compile(CompilationInput input)
    {
        var compilationOptions = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary, 
                concurrentBuild: false, // WASM does not support concurrent builds
                optimizationLevel: OptimizationLevel.Release)
            .WithUsings(input.UsingStatementNamespaces);

        var sourceCodeTree = SyntaxFactory.ParseSyntaxTree(input.SourceCode.Trim());
        var unitTestTree = SyntaxFactory.ParseSyntaxTree(input.TestCode);
        var injectionTrees = GetInstrumentationSyntaxTrees();

        var isoDateTime = DateTime.Now.ToString("yyyyMMddTHHmmss");
        var compilation = CSharpCompilation.Create($"PlaygroundBuild-{isoDateTime}.dll")
            .WithOptions(compilationOptions)
            .WithReferences(input.References)
            .AddSyntaxTrees(sourceCodeTree, unitTestTree)
            .AddSyntaxTrees(injectionTrees);

        await using var codeStream = new MemoryStream();
        
        var result = compilation.Emit(codeStream);

        return new CompilationResult()
        {
            Diagnostics = result.Diagnostics,
            Success = result.Success,
            EmittedBytes = result.Success ? codeStream.ToArray() : null,
        };
    }
    
    private static List<SyntaxTree> GetInstrumentationSyntaxTrees()
    {
        var trees = new List<SyntaxTree>();
        
        foreach (var (name, code) in CodeInjection.MutantHelpers)
        {
            var tree = CSharpSyntaxTree.ParseText(code, path: name, encoding: Encoding.UTF32);
            trees.Add(tree);
        }

        return trees;
    }
}