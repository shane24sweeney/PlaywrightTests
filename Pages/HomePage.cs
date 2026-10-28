using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace PlaywrightTests.Pages;

public class HomePage(IPage page) : BasePage(page)
{
    protected override string Path => "/home";

    private ILocator Main => Page.GetByRole(AriaRole.Main);

    public ILocator SummaryHeading => Main.GetByRole(AriaRole.Heading, new() { Name = "Professional Summary" });
    public ILocator ContactMeLink => Main.GetByRole(AriaRole.Link, new() { Name = "Contact me" });
    public ILocator LinkedInLink => Main.GetByRole(AriaRole.Link, new() { Name = "LinkedIn" });
    public ILocator GitHubLink => Main.GetByRole(AriaRole.Link, new() { Name = "GitHub" });

    // Carousel controls. Clicking Next/Previous also pauses the slideshow.
    public ILocator NextSlideButton => Page.GetByRole(AriaRole.Button, new() { Name = "Next slide" });
    public ILocator PreviousSlideButton => Page.GetByRole(AriaRole.Button, new() { Name = "Previous slide" });
    public ILocator PauseButton => Page.GetByRole(AriaRole.Button, new() { Name = "Pause slideshow" });
    public ILocator PlayButton => Page.GetByRole(AriaRole.Button, new() { Name = "Play slideshow" });
    public ILocator SlidePosition => Main.GetByText(new Regex(@"^\d+ of \d+$"));

    public async Task<ContactPage> ClickContactMeAsync()
    {
        await ContactMeLink.ClickAsync();
        return new ContactPage(Page);
    }

    public async Task NextSlideAsync() => await NextSlideButton.ClickAsync();
    public async Task PreviousSlideAsync() => await PreviousSlideButton.ClickAsync();
    public async Task PauseSlideshowAsync() => await PauseButton.ClickAsync();
    public async Task PlaySlideshowAsync() => await PlayButton.ClickAsync();

    // Parses the "2 of 3" indicator.
    public async Task<(int Current, int Total)> GetSlidePositionAsync()
    {
        var parts = (await SlidePosition.TextContentAsync())!.Split(" of ");
        return (int.Parse(parts[0]), int.Parse(parts[1]));
    }
}
