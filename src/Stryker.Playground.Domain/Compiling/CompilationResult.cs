using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Stryker.Playground.Domain.Compiling;

public class CompilationResult
{
    public ImmutableArray<Diagnostic> Diagnostics { get; set; }
    public bool Success { get; set; }
    public byte[]? EmittedBytes { get; set; }
}