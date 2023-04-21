using BlazorMonaco.Editor;
using XtermBlazor;

namespace Stryker.Playground.WebAssembly.Pages;

public partial class Playground
{
    private StandaloneCodeEditor SourceCodeEditor { get; set; }  = default!;
    
    private StandaloneCodeEditor TestCodeEditor { get; set; }  = default!;
    
    private Xterm Terminal { get; set; }  = default!;
    

    private async Task OnFirstRender()
    {
        for (var i = 0; i < 20; i++)
        {
            await Terminal.WriteLine("Hello " + i);
            await Terminal.ScrollToBottom();
        }
    }
}

