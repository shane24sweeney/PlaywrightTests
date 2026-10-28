using Microsoft.Playwright;

namespace PlaywrightTests.Pages;

// Any page on the site, for tests that only check the shared layout (nav and heading).
public class SitePage(IPage page, string path) : BasePage(page)
{
    protected override string Path => path;
}
