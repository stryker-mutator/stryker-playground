namespace Stryker.Core.Common.Mutants
{
    public enum MutantStatus
    {
        NotRun,
        Killed,
        Survived,
        Timeout,
        CompileError,
        Ignored,
        NoCoverage
    }
}
