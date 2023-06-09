﻿using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Logging.Abstractions;
using Stryker.Core.Common.InjectedHelpers;
using Stryker.Core.Common.Logging;
using Stryker.Core.Common.Mutants;
using Stryker.Playground.Domain.Compiling.Rollback;

namespace Stryker.Playground.Domain.Compiling;

public class PlaygroundCompiler : IPlaygroundCompiler
{
    private const int MaxAttempt = 50;
    
    private IRollbackProcess _rollbackProcess;

    public PlaygroundCompiler()
    {
        _rollbackProcess = new RollbackProcess();
        ApplicationLogging.LoggerFactory = NullLoggerFactory.Instance;
    }
    
    public async Task<MutantCompilationResult> CompileWithMutations(CompilationInput input)
    {
        var orchestrator = new CsharpMutantOrchestrator();

        var sourceCodeRoot = input.SourceCode;

        Console.WriteLine("Original syntax tree:");
        Console.WriteLine(sourceCodeRoot.ToFullString());

        var mutatedTree = orchestrator.Mutate(sourceCodeRoot);

        var mutatedInput = new CompilationInput()
        {
            References = input.References,
            TestCode = input.TestCode,
            SourceCode = await mutatedTree.SyntaxTree.GetRootAsync(),
        };
        
        Console.WriteLine($"Mutated the syntax tree with {orchestrator.MutantCount} mutations:");
        Console.WriteLine(mutatedTree.ToFullString());
        
        var compilation = GetCompilation(mutatedInput);
        
        using var ilStream = new MemoryStream();

        // first try compiling
        var retryCount = 1;
        (var rollbackProcessResult, var emitResult, retryCount) = TryCompilation(ilStream, compilation, null, false, retryCount);

        // If compiling failed and the error has no location, log and throw exception.
        if (!emitResult.Success && emitResult.Diagnostics.Any(diagnostic => diagnostic.Location == Location.None && diagnostic.Severity == DiagnosticSeverity.Error))
        {
            Console.WriteLine("Failed to build the mutated assembly due to unrecoverable error: {0}",
                emitResult.Diagnostics.First(diagnostic => diagnostic.Location == Location.None && diagnostic.Severity == DiagnosticSeverity.Error));
            throw new AggregateException("General Build Failure detected.");
        }

        for (var count = 1; !emitResult.Success && count < MaxAttempt; count++)
        {
            // compilation did not succeed. let's compile a couple times more for good measure
            (rollbackProcessResult, emitResult, retryCount) = TryCompilation(ilStream, rollbackProcessResult?.Compilation ?? compilation, emitResult, retryCount == MaxAttempt - 1, retryCount);
        }


        var rolledBackIds = rollbackProcessResult?.RollbackedIds?.ToList() ?? new List<int>();
        
        foreach (var mutant in orchestrator.Mutants)
        {
            if (rolledBackIds.Contains(mutant.Id))
            {
                mutant.ResultStatus = MutantStatus.CompileError;
            }
        }

        return new MutantCompilationResult
        {
            OriginalTree = input.SourceCode,
            Mutants = orchestrator.Mutants,
            Diagnostics = emitResult.Diagnostics,
            EmittedBytes = ilStream.ToArray(),
            Success = emitResult.Success,
        };
    }
    
    public async Task<CompilationResult> Compile(CompilationInput input)
    {
        var compilation = GetCompilation(input);
        await using var codeStream = new MemoryStream();
        
        var result = compilation.Emit(codeStream);

        return new CompilationResult()
        {
            Diagnostics = result.Diagnostics,
            Success = result.Success,
            EmittedBytes = result.Success ? codeStream.ToArray() : null,
        };
    }

    private static CSharpCompilation GetCompilation(CompilationInput input)
    {
        var compilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            concurrentBuild: false, // WASM does not support concurrent builds
            optimizationLevel: OptimizationLevel.Release);
        
        var isoDateTime = DateTime.Now.ToString("yyyyMMddTHHmmss");

        return CSharpCompilation.Create($"PlaygroundBuild-{isoDateTime}")
            .WithOptions(compilationOptions)
            .WithReferences(input.References)
            .AddSyntaxTrees(input.SourceCode.SyntaxTree.WithFilePath("Program.cs"))
            .AddSyntaxTrees(input.TestCode.SyntaxTree.WithFilePath("Tests.cs"))
            .AddSyntaxTrees(GetGlobalUsingsSyntaxTree(input.GlobalUsingDirectives).WithFilePath("Globals.cs"))
            .AddSyntaxTrees(GetInstrumentationSyntaxTrees());
    }

    private (RollbackProcessResult, EmitResult, int) TryCompilation(
        Stream ms,
        CSharpCompilation compilation,
        EmitResult? previousEmitResult,
        bool lastAttempt,
        int retryCount)
    {
        RollbackProcessResult rollbackProcessResult = null;

        if (previousEmitResult != null)
        {
            // remove broken mutations
            rollbackProcessResult = _rollbackProcess.Start(compilation, previousEmitResult.Diagnostics, lastAttempt, false);
            compilation = rollbackProcessResult.Compilation;
        }

        // reset the memoryStream
        ms.SetLength(0);

        var emitResult = compilation.Emit(ms);

        LogEmitResult(emitResult);

        return (rollbackProcessResult, emitResult, ++retryCount);
    }

    private void LogEmitResult(EmitResult result)
    {
        if (!result.Success)
        {
            Console.WriteLine("Compilation failed");

            foreach (var err in result.Diagnostics.Where(x => x.Severity is DiagnosticSeverity.Error))
            {
                Console.WriteLine("{0}, {1}", err?.GetMessage() ?? "No message", err?.Location.SourceTree?.FilePath ?? "Unknown filepath");
            }
        }
        else
        {
            Console.WriteLine("Compilation successful");
        }
    }
    
    private static SyntaxTree GetGlobalUsingsSyntaxTree(string[] namespaces)
    {
        var fileContent = string.Join("\n", namespaces.Select(ns => $"global using {ns};"));

        return SyntaxFactory.ParseSyntaxTree(fileContent);
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