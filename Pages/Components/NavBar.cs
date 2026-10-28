using Microsoft.Playwright;

namespace PlaywrightTests.Pages.Components;

// The primary navigation bar shown on every page.
public class NavBar(IPage page)
{
    public ILocator Root => page.GetByRole(AriaRole.Navigation, new() { Name = "Primary navigation" });

    public ILocator Link(string name) => Root.GetByRole(AriaRole.Link, new() { Name = name, Exact = true });

    // Links marked as the page being viewed; there should only ever be one.
    public ILocator CurrentLinks => Root.Locator("[aria-current=page]");

    public async Task ClickLinkAsync(string name) => await Link(name).ClickAsync();

    public async Task<ContactPage> GoToContactAsync()
    {
        await ClickLinkAsync("Contact");
        return new ContactPage(page);
    }
}
