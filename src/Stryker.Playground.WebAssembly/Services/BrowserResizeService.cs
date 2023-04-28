using Microsoft.JSInterop;

namespace Stryker.Playground.WebAssembly.Services;

public class BrowserResizeService
{
    private readonly IJSRuntime _jsRuntime;

    public BrowserResizeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public static event Func<Task>? OnResize;

    [JSInvokable]
    public static async Task OnBrowserResize()
    {
        if (OnResize is not null)
        {
            await OnResize.Invoke(); 
        }
    }

    public async Task<int> GetInnerHeight()
    {
        return await _jsRuntime.InvokeAsync<int>("browserResize.getInnerHeight");
    }

    public async Task<int> GetInnerWidth()
    {
        return await _jsRuntime.InvokeAsync<int>("browserResize.getInnerWidth");
    }
}