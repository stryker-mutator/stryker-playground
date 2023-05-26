using Microsoft.Playwright;

namespace Stryker.Playground.WebAssembly.Tests;

public class DefaultMutationFlowTest
{
    [Fact]
    public async Task VerifyMutationTestResult()
    {
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();
        
        await page.GotoAsync("http://localhost:5000/");
        
        // Wait for playground to initialize
        await page.WaitForSelectorAsync("button:has-text(\"Run Mutation Tests\"):not([disabled])");
        
        // Run mutation tests
        await page.ClickAsync("button:has-text(\"Run Mutation Tests\")");

        // Wait for tests to finish & report to display. Then navigate back to editor screen
        await page.WaitForSelectorAsync("button:has-text(\"Mutation Report\").active", new PageWaitForSelectorOptions() { Timeout = 60_000 * 5});
        await page.ClickAsync("button:has-text(\"Editor\")");

        // Validate terminal output
        var terminalContent = await page.InnerTextAsync(".xterm-rows");
        var expectedText = "Your mutation score is"
            .Replace(" ", ""); // Strip spaces because InnerTextAsync does not preserve spaces from the xterm component..
        
        Assert.Contains(expectedText, terminalContent);
    }
}