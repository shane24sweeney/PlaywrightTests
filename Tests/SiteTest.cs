using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework.Interfaces;

namespace PlaywrightTests.Tests;

// Base class for selenium-automation.com tests: lets page objects use relative paths.
public abstract class SiteTest : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = "https://selenium-automation.com",
    };

    // Record a trace for every test; keep it only when the test fails.
    // Open one with: pwsh bin/Debug/net10.0/playwright.ps1 show-trace <file>.zip
    [SetUp]
    public async Task StartTracing()
    {
        await Context.Tracing.StartAsync(new()
        {
            Title = TestContext.CurrentContext.Test.FullName,
            Screenshots = true,
            Snapshots = true,
            Sources = true,
        });
    }

    [TearDown]
    public async Task StopTracing()
    {
        var test = TestContext.CurrentContext;
        if (test.Result.Outcome.Status != TestStatus.Failed)
        {
            await Context.Tracing.StopAsync();
            return;
        }

        var fileName = string.Join("_", test.Test.FullName.Split(Path.GetInvalidFileNameChars())) + ".zip";
        var tracePath = Path.Combine(test.WorkDirectory, "playwright-traces", fileName);
        await Context.Tracing.StopAsync(new() { Path = tracePath });
        TestContext.AddTestAttachment(tracePath, "Playwright trace");
    }
}
