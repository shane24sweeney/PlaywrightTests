using System.Text.RegularExpressions;
using Microsoft.Playwright;
using PlaywrightTests.Pages;

namespace PlaywrightTests.Tests;

[Category(TestCategories.Positive)]
public class HomePageTests : SitePageTest<HomePage>
{
    protected override HomePage CreatePage(IPage page) => new(page);

    [Test]
    public async Task ShowsNameAndSummary()
    {
        await Expect(StartPage.Heading).ToHaveTextAsync("Shane James Sweeney");
        await Expect(StartPage.SummaryHeading).ToBeVisibleAsync();
    }

    [Test]
    public async Task ContactMeOpensContactPage()
    {
        var contact = await StartPage.ClickContactMeAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/contact$"));
        await Expect(contact.Heading).ToHaveTextAsync("Send me a message.");
    }

    [Test]
    public async Task SocialLinksPointToProfiles()
    {
        await Expect(StartPage.LinkedInLink).ToHaveAttributeAsync("href", "https://linkedin.com/in/shane-sweeney-37a934135");
        await Expect(StartPage.GitHubLink).ToHaveAttributeAsync("href", "https://github.com/shane24sweeney");
    }

    [Test]
    public async Task NextSlideAdvancesCarousel()
    {
        // Pause first so autoplay can't move the slide between reading and clicking.
        await StartPage.PauseSlideshowAsync();
        var (current, total) = await StartPage.GetSlidePositionAsync();

        await StartPage.NextSlideAsync();

        var expected = current % total + 1;
        await Expect(StartPage.SlidePosition).ToHaveTextAsync($"{expected} of {total}");
    }

    [Test]
    public async Task PreviousSlideMovesCarouselBack()
    {
        await StartPage.PauseSlideshowAsync();
        var (current, total) = await StartPage.GetSlidePositionAsync();

        await StartPage.PreviousSlideAsync();

        var expected = current == 1 ? total : current - 1;
        await Expect(StartPage.SlidePosition).ToHaveTextAsync($"{expected} of {total}");
    }

    [Test]
    public async Task PauseAndPlayToggleSlideshow()
    {
        await StartPage.PauseSlideshowAsync();
        await Expect(StartPage.PlayButton).ToBeVisibleAsync();

        await StartPage.PlaySlideshowAsync();
        await Expect(StartPage.PauseButton).ToBeVisibleAsync();
    }
}
