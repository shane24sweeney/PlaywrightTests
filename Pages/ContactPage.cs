using Microsoft.Playwright;

namespace PlaywrightTests.Pages;

public class ContactPage(IPage page) : BasePage(page)
{
    protected override string Path => "/contact";

    public ILocator NameInput => Page.GetByLabel("Name", new() { Exact = true });
    public ILocator EmailInput => Page.GetByLabel("E-mail", new() { Exact = true });
    public ILocator MessageInput => Page.GetByLabel("Message", new() { Exact = true });
    public ILocator SubmitButton => Page.GetByRole(AriaRole.Button, new() { Name = "Submit" });

    // Result text shown under the form after a submit attempt.
    public ILocator StatusMessage => Page.Locator(".contact-status");

    // Max lengths enforced by the form's maxlength attributes.
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 254;
    public const int MessageMaxLength = 5000;

    public const string ApiPath = "/api/contact";

    public async Task FillFormAsync(string name, string email, string message)
    {
        await NameInput.FillAsync(name);
        await EmailInput.FillAsync(email);
        await MessageInput.FillAsync(message);
    }

    public async Task SubmitAsync() => await SubmitButton.ClickAsync();

    // Submits the form and returns the request it sends to the contact API.
    public async Task<IRequest> SubmitAndCaptureRequestAsync() =>
        await Page.RunAndWaitForRequestAsync(SubmitAsync,
            request => request.Method == "POST" && request.Url.EndsWith(ApiPath));

    public Task<bool> IsNameValidAsync() => IsValidAsync(NameInput);
    public Task<bool> IsEmailValidAsync() => IsValidAsync(EmailInput);
    public Task<bool> IsMessageValidAsync() => IsValidAsync(MessageInput);

    public async Task<(bool Name, bool Email, bool Message)> GetValidityAsync() =>
        (await IsNameValidAsync(), await IsEmailValidAsync(), await IsMessageValidAsync());

    // The form relies on native HTML5 validation (required, type="email").
    private static async Task<bool> IsValidAsync(ILocator field) =>
        await field.EvaluateAsync<bool>("e => e.validity.valid");
}
