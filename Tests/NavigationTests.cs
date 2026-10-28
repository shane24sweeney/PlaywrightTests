using System.Text.RegularExpressions;
using Microsoft.Playwright;
using PlaywrightTests.Pages;

namespace PlaywrightTests.Tests;

public class NavigationTests : SitePageTest<HomePage>
{
    protected override HomePage CreatePage(IPage page) => new(page);

    [TestCase("Contact", "/contact", "Send me a message.")]
    [TestCase("About", "/about", "About Shane")]
    [TestCase("Resume", "/resume", "Professional Experience")]
    [TestCase("Testimonials", "/testimonials", "Testimonials")]
    [TestCase("Education", "/education", "Education")]
    [TestCase("Charity Work", "/charity", "Charity Work")]
    [Category(TestCategories.Positive)]
    public async Task NavLinkOpensPage(string linkName, string path, string heading)
    {
        await StartPage.Nav.ClickLinkAsync(linkName);

        var landed = new SitePage(Page, path);
        await Expect(Page).ToHaveURLAsync(new Regex($"{path}$"));
        await Expect(landed.Heading).ToHaveTextAsync(heading);
        await Expect(landed.Nav.Link(linkName)).ToHaveAttributeAsync("aria-current", "page");
    }

    [Test, Category(TestCategories.Positive)]
    public async Task ContactLinkOpensContactPage()
    {
        var contact = await StartPage.Nav.GoToContactAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/contact$"));
        await Expect(contact.Heading).ToHaveTextAsync("Send me a message.");
        await Expect(contact.SubmitButton).ToBeVisibleAsync();
    }

    [Test, Category(TestCategories.Positive)]
    public async Task HomeLinkIsMarkedCurrentOnHomePage()
    {
        await Expect(StartPage.Nav.Link("Home")).ToHaveAttributeAsync("aria-current", "page");
    }

    [TestCase("Home")]
    [TestCase("Contact")]
    [TestCase("Resume")]
    [Category(TestCategories.Negative)]
    public async Task OnlyOneNavLinkIsMarkedCurrent(string linkName)
    {
        if (linkName != "Home")
            await StartPage.Nav.ClickLinkAsync(linkName);

        await Expect(StartPage.Nav.CurrentLinks).ToHaveCountAsync(1);
        await Expect(StartPage.Nav.CurrentLinks).ToHaveTextAsync(linkName);
    }

    [TestCase("/does-not-exist")]
    [TestCase("/admin")]
    [Category(TestCategories.Negative)]
    public async Task UnknownPageRedirectsHome(string path)
    {
        var unknown = new SitePage(Page, path);
        await unknown.GotoAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/home$"));
        await Expect(unknown.Heading).ToHaveTextAsync("Shane James Sweeney");
        await Expect(unknown.Nav.Link("Home")).ToHaveAttributeAsync("aria-current", "page");
    }
}
