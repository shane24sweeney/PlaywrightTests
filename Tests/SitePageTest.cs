using Microsoft.Playwright;
using PlaywrightTests.Pages;

namespace PlaywrightTests.Tests;

// Fixture for tests that start on one page: opens TPage before each test and exposes it as StartPage.
public abstract class SitePageTest<TPage> : SiteTest where TPage : BasePage
{
    protected TPage StartPage { get; private set; } = null!;

    protected abstract TPage CreatePage(IPage page);

    [SetUp]
    public async Task OpenStartPage()
    {
        StartPage = CreatePage(Page);
        await StartPage.GotoAsync();
    }
}
