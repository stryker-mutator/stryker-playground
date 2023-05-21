using BlazorMonaco.Editor;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.JSInterop;
using SpawnDev.BlazorJS.WebWorkers;
using Stryker.Core.Common.Mutants;
using Stryker.Core.Common.Options;
using Stryker.Core.Common.Reporters.Json;
using Stryker.Playground.Domain;
using Stryker.Playground.Domain.Compiling;
using Stryker.Playground.Domain.TestRunners;
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
        
        await Terminal.WriteAndScroll($"Generated {mutants.Count} mutants");

        foreach (var mutant in mutatedCompilation.Mutants)
        {
            await Terminal.Write($"Running tests for mutant {mutant.DisplayName}");
            var testResult = await RunTests(mutatedCompilation, mutant.Id, true);

            mutant.ResultStatus = testResult.Status switch
            {
                TestRunStatus.FAILED => MutantStatus.Killed,
                TestRunStatus.TIMEOUT => MutantStatus.Timeout,
                TestRunStatus.PASSED => MutantStatus.Survived,
                _ => MutantStatus.NotRun,
            };

            await Terminal.WriteAndScroll($" ({mutant.ResultStatus.ToString()})");
        }
        
        var mutationScore = ((double)mutatedCompilation.Mutants.Count(x => x.ResultStatus != MutantStatus.Survived) / mutants.Count) * 100;

        await Terminal.DisplayMutationScore(mutationScore);

        var projectComponent = ProjectComponentBuilder.BuildProjectComponent(mutatedCompilation.OriginalTree, mutants);

        _jsonReport = JsonReport.Build(new StrykerOptions(), projectComponent);

        await DisplayMutationReport();
    }
    
    public async Task ExecuteUnitTests()
    {
        await Terminal.Clear();
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
        var worker = await WebWorkerService.GetWebWorker() ?? throw new ApplicationException("Unable to start web worker");
        var testWorker = worker.GetService<ITestRunner>();

        try
        {
            return await testWorker
                .RunTests(compilation.EmittedBytes!, activeMutantId, stopOnError)
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

        foreach (var diagnostic in diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error))
        {
            await Terminal.Error(diagnostic.ToString());
        }
    }

    private async Task<CompilationInput> GetInput()
    {
        return new CompilationInput
        {
            References = _references,
            SourceCode = await SourceCodeEditor.GetValue(),
            TestCode = await TestCodeEditor.GetValue(),
            UsingStatementNamespaces = CompilationInput.DefaultNamespaces,
        };
    }

    protected override async Task OnInitializedAsync()
    {
        await JsRuntime.InvokeAsync<object>("browserResize.registerResizeCallback");
        BrowserResizeService.OnResize += OnBrowserResize;

        await base.OnInitializedAsync();
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

