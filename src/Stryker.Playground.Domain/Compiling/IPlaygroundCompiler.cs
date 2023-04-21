namespace Stryker.Playground.Domain.Compiling;

public interface IPlaygroundCompiler
{
    public Task<CompilationResult> Compile(CompilationInput input);
    
    public Task<MutantCompilationResult> CompileWithMutations(CompilationInput input);
}