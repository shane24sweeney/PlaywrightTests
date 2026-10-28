using Microsoft.Playwright;
using PlaywrightTests.Pages.Components;

namespace PlaywrightTests.Pages;

// Elements shared by every page on the site: primary nav and the page heading.
public abstract class BasePage
{
    protected readonly IPage Page;

    protected BasePage(IPage page)
    {
        Page = page;
        Nav = new NavBar(page);
    }

    // Relative to the BaseURL configured in SiteTest.
    protected abstract string Path { get; }

    public NavBar Nav { get; }

    public ILocator Heading => Page.GetByRole(AriaRole.Heading, new() { Level = 1 });

    public async Task GotoAsync() => await Page.GotoAsync(Path);
}
