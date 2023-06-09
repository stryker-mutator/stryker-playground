using System.Net.Http.Json;
using BlazorMonaco;
using BlazorMonaco.Editor;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.JSInterop;
using SpawnDev.BlazorJS.WebWorkers;
using Stryker.Core.Common.Mutants;
using Stryker.Core.Common.Options;
using Stryker.Core.Common.Reporters.Json;
using Stryker.Playground.Domain;
using Stryker.Playground.Domain.Compiling;
using Stryker.Playground.Domain.TestRunners;
using Stryker.Playground.WebAssembly.Github;
using Stryker.Playground.WebAssembly.Services;
using XtermBlazor;

namespace Stryker.Playground.WebAssembly.Pages;

public partial class Playground
{
    [Inject]
    public WebWorkerService WebWorkerService { get; set; } = default!;

    [Inject]
    public HttpClient HttpClient { get; set; } = default!;

    [Inject] 
    public NavigationManager NavManager { get; set; } = default!;

    [Inject]
    public IJSRuntime JsRuntime { get; set; } = default!;

    [Inject] 
    private BrowserResizeService Browser { get; set; } = default!;
    
    private StandaloneCodeEditor SourceCodeEditor { get; set; }  = default!;
    
    private StandaloneCodeEditor TestCodeEditor { get; set; }  = default!;
    
    private Xterm Terminal { get; set; }  = default!;

    private bool Busy { get; set; } = true;
    private bool Initialized { get; set; } = false;
    
    private JsonReport? _jsonReport = null;
    private bool _displayReport = false;

    public bool DisplayReport => _jsonReport is not null && _displayReport;

    private readonly IPlaygroundCompiler _compiler = new PlaygroundCompiler();

    private readonly List<MetadataReference> _references = new();

    public async Task OnClick_StartMutationTests()
    {
        if (Busy)
        {
            return;
        }
        
        _displayReport = false;
        Busy = true;
        await ExecuteMutationTests();
        Busy = false;
    }
    
    public async Task OnClick_StartUnitTests()
    {
        if (Busy)
        {
            return;
        }

        _displayReport = false;
        Busy = true;
        await ExecuteUnitTests();
        Busy = false;
    }

    public async Task ExecuteMutationTests()
    {
        var input = await GetInput();
        
        await Terminal.Clear();
        await TestCodeEditor.ResetDeltaDecorations();
        await SourceCodeEditor.ResetDeltaDecorations();
        
        await Terminal.WriteAndScroll("Performing initial compilation (without mutating the source)");
        
        var initialCompilation = await _compiler.Compile(input);

        if (!initialCompilation.Success || initialCompilation.EmittedBytes is null)
        {
            await DisplayCompilationErrors(initialCompilation.Diagnostics.ToList());
            return;
        }

        await Terminal.WriteAndScroll("Performing initial test run (without mutants)");
        
        var testResults = await RunTests(initialCompilation);

        if (testResults.Status != TestRunStatus.PASSED)
        {
            await Terminal.WriteAndScroll(testResults.GetResultMessage());
            await Terminal.Error("Initial test run failed");
            return;
        }
        
        await Terminal.WriteAndScroll("Mutating your source..");

        var mutatedCompilation = await _compiler.CompileWithMutations(input);
        var mutants = mutatedCompilation.Mutants.ToList();

        if (mutants.Count == 0)
        {
            await Terminal.Error("Error: The supplied code does not contain any potential locations where mutations can be applied.\n" +
                                 "Ensure that the code includes variables, expressions, or operations that are eligible for mutation.");
            return;
        }

        // Perform mutation test dry run with no active mutants in order to capture mutant coverage information
        var dryRunResults = await RunTests(mutatedCompilation, -1, true);
        
        if (dryRunResults.CoveredMutantIds is not null)
        {
            foreach (var mutant in mutants.Where(mutant => mutant.ResultStatus == MutantStatus.NotRun && !dryRunResults.CoveredMutantIds.Contains(mutant.Id)))
            {
                mutant.ResultStatus = MutantStatus.NoCoverage;
            }
        }
        
        await Terminal.WriteAndScroll($"Generated a total of {mutants.Count} mutants");

        if (mutants.Any(x => x.ResultStatus == MutantStatus.CompileError))
        {
            await Terminal.WriteAndScroll($"{mutants.Count(x => x.ResultStatus == MutantStatus.CompileError)} mutants have status CompileError and will be skipped");
        }
        
        if (mutants.Any(x => x.ResultStatus == MutantStatus.NoCoverage))
        {
            await Terminal.WriteAndScroll($"{mutants.Count(x => x.ResultStatus == MutantStatus.NoCoverage)} mutants have status NoCoverage and will be skipped");
        }

        foreach (var mutant in mutants.Where(x => x.ResultStatus != MutantStatus.CompileError && 
                                                  x.ResultStatus != MutantStatus.NoCoverage))
        {
            await Terminal.Write($"Testing mutant {"#" + mutant.DisplayName,-32}");
            var testResult = await RunTests(mutatedCompilation, mutant.Id, true);

            mutant.ResultStatus = testResult.Status switch
            {
                TestRunStatus.FAILED => MutantStatus.Killed,
                TestRunStatus.TIMEOUT => MutantStatus.Timeout,
                TestRunStatus.PASSED => MutantStatus.Survived,
                _ => MutantStatus.NotRun,
            };

            var statusIcon = mutant.ResultStatus switch
            {
                MutantStatus.Killed => $"✅",
                MutantStatus.Survived => "👽",
                MutantStatus.Timeout => "⏳",
                MutantStatus.NoCoverage => "🙈",
                MutantStatus.Ignored => "🤥",
                MutantStatus.CompileError => "💥",
                _ => "❔",
            };

            await Terminal.WriteAndScroll($" {statusIcon}  {mutant.ResultStatus.ToString()}");
        }

        var detectedCount = mutants.Count(m => m.ResultStatus is MutantStatus.Killed or MutantStatus.Timeout);
        var undetectedCount = mutants.Count(m => m.ResultStatus is MutantStatus.Survived or MutantStatus.NoCoverage);
        var totalCount = detectedCount + undetectedCount;
        
        var mutationScore = ((double)detectedCount / totalCount) * 100;

        await Terminal.DisplayMutationScore(mutationScore);

        var projectComponent = ProjectComponentBuilder.BuildProjectComponent(mutatedCompilation.OriginalTree.SyntaxTree, mutants);

        _jsonReport = JsonReport.Build(new StrykerOptions(), projectComponent);

        await DisplayMutationReport();
    }
    
    public async Task ExecuteUnitTests()
    {
        await Terminal.Clear();
        await TestCodeEditor.ResetDeltaDecorations();
        await SourceCodeEditor.ResetDeltaDecorations();
        await Terminal.WriteAndScroll("Compiling..");

        var compilation = await _compiler.Compile(await GetInput());

        if (!compilation.Success || compilation.EmittedBytes is null)
        {
            await DisplayCompilationErrors(compilation.Diagnostics.ToList());
            return;
        }

        await Terminal.WriteAndScroll("Running unit tests..");
        
        var testResults = await RunTests(compilation);

        foreach (var line in testResults.TextOutput)
        {
            await Terminal.WriteAndScroll(line);
        }

        await Terminal.WriteAndScroll(testResults.GetResultMessage());
    }

    private async Task<TestRunResult> RunTests(CompilationResult compilation, int? activeMutantId = null, bool stopOnError = false)
    {
        var assemblyBytes = compilation.EmittedBytes ?? throw new ApplicationException("Unable to start test run: EmittedBytes is null!");
        var worker = await WebWorkerService.GetWebWorker() ?? throw new ApplicationException("Unable to start web worker");
        var testWorker = worker.GetService<ITestRunner>();
        
        try
        {
            return await testWorker
                .RunTests(assemblyBytes, activeMutantId, stopOnError)
                .WaitAsync(PlaygroundConstants.TestSuiteMaxDuration);
        }
        catch (TimeoutException)
        {
            return new TestRunResult()
            {
                Status = TestRunStatus.TIMEOUT,
            };
        }
        finally
        {
            worker.Dispose();
            worker = null;
        }
    }

    private async Task DisplayCompilationErrors(List<Diagnostic> diagnostics)
    {
        var errorCount = diagnostics.Count(x => x.Severity == DiagnosticSeverity.Error);
        var warnCount = diagnostics.Count(x => x.Severity == DiagnosticSeverity.Warning);
        
        await Terminal.Error($"Compilation failed with {errorCount} errors and {warnCount} warnings");

        var sourceDecorations = new List<ModelDeltaDecoration>();
        var testDecorations = new List<ModelDeltaDecoration>();

        foreach (var diagnostic in diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error))
        {
            await Terminal.Error(diagnostic.ToString());
            
            var range = new BlazorMonaco.Range()
            {
                StartLineNumber = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1,
                EndLineNumber = diagnostic.Location.GetLineSpan().EndLinePosition.Line + 1,
                StartColumn = diagnostic.Location.GetLineSpan().StartLinePosition.Character + 1,
                EndColumn = diagnostic.Location.GetLineSpan().EndLinePosition.Character + 1
            };

            var decor = new ModelDeltaDecoration
            {
                Range = range,
                Options = new ModelDecorationOptions
                {
                    IsWholeLine = false,
                    ClassName = "squiggly-underline",
                    HoverMessage = new MarkdownString[]
                    {
                        new() { Value = $"[{diagnostic.Id}]" },
                        new() { Value = diagnostic.GetMessage()},
                    }
                }
            };

            if (diagnostic.Location.SourceTree?.FilePath == "Program.cs")
            {
                sourceDecorations.Add(decor);
            }
            else if (diagnostic.Location.SourceTree?.FilePath == "Tests.cs")
            {
                testDecorations.Add(decor);
            }
        }
        
        await SourceCodeEditor.DeltaDecorations(Array.Empty<string>(), sourceDecorations.ToArray());
        await TestCodeEditor.DeltaDecorations(Array.Empty<string>(), testDecorations.ToArray());
    }

    private async Task<CompilationInput> GetInput()
    {
        return new CompilationInput
        {
            References = _references,
            SourceCode = await SyntaxFactory.ParseSyntaxTree(await SourceCodeEditor.GetValue()).GetRootAsync(),
            TestCode = await SyntaxFactory.ParseSyntaxTree(await TestCodeEditor.GetValue()).GetRootAsync(),
        };
    }

    protected override async Task OnInitializedAsync()
    {
        await JsRuntime.InvokeAsync<object>("browserResize.registerResizeCallback");
        BrowserResizeService.OnResize += OnBrowserResize;

        await base.OnInitializedAsync();
    }
    
    private async Task<GistData?> GetGistData(string gistId)
    {
        var response = await HttpClient.GetAsync($"https://api.github.com/gists/{gistId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GistData>();
        }

        return null;
    }

    private async Task OnFirstRender()
    {
        await Terminal.WriteAndScroll("Loading dependencies, please wait..");

        foreach (var lib in CompilationInput.DefaultLibraries)
        {
            try
            {
                await LoadLibrary(lib + ".dll");
            }
            catch (Exception e)
            {
                await Terminal.Error("Initialization failed! Unable to load system libraries");
                await Terminal.WriteAndScroll(e.Message);
                return;
            }
        }
        
        var gistId = NavManager.QueryString("gist");

        if (!string.IsNullOrEmpty(gistId))
        {
            var gistData = await GetGistData(gistId) ?? new GistData();

            if (gistData.Files.TryGetValue("Program.cs", out var gistSrcFile) && !string.IsNullOrEmpty(gistSrcFile.Content))
            {
                await SourceCodeEditor.SetValue(gistSrcFile.Content);
            }
            
            if (gistData.Files.TryGetValue("Tests.cs", out var gistTestFile) && !string.IsNullOrEmpty(gistTestFile.Content))
            {
                await TestCodeEditor.SetValue(gistTestFile.Content);
            }
        }

        Initialized = true;
        Busy = false;
        
        await Terminal.Clear();

        foreach (var line in PlaygroundConstants.WelcomeMessageLines)
        {
            await Terminal.WriteAndScroll(line);
        }

        await Terminal.Focus();

        await SourceCodeEditor.Layout();
        await TestCodeEditor.Layout();
    }

    private async Task OnClick_EditorButton()
    {
        _displayReport = false;
        await Task.Delay(200);
        await OnBrowserResize();
    }

    private async Task OnBrowserResize()
    {
        Console.WriteLine("Adjusting editor sizes!");
        await SourceCodeEditor.Layout();
        await TestCodeEditor.Layout();
    }

    private async Task DisplayMutationReport()
    {
        await JsRuntime.InvokeVoidAsync("setMutationReport", _jsonReport?.ToJsonHtmlSafe());
        _displayReport = true;
    }

    private async Task LoadLibrary(string lib)
    {
        try
        {
            await using var referenceStream = await HttpClient.GetStreamAsync($"/_framework/{lib}");
            _references.Add(MetadataReference.CreateFromStream(referenceStream));
        }
        catch (Exception)
        {
            // TODO: Look into setting this as an environment variable
            const string staticFileHost = "https://stryker-mutator.io/stryker-playground/";
            await using var referenceStream = await HttpClient.GetStreamAsync($"{staticFileHost}/_framework/{lib}");
            _references.Add(MetadataReference.CreateFromStream(referenceStream));
        }
    }
}

