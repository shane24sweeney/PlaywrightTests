using System.Text.RegularExpressions;
using Microsoft.Playwright;
using PlaywrightTests.Pages;

namespace PlaywrightTests.Tests;

public class ContactPageTests : SitePageTest<ContactPage>
{
    private const string ValidName = "Test User";
    private const string ValidEmail = "test.user@example.com";
    private const string ValidMessage = "Hello from an automated test.";

    // Every POST the page tries to send to the contact API (all are aborted, see below).
    private readonly List<IRequest> _contactRequests = [];

    protected override ContactPage CreatePage(IPage page) => new(page);

    // Runs after the base fixture opens the page; the page load is a GET, so nothing is lost.
    [SetUp]
    public async Task BlockNonGetRequests()
    {
        Page.Request += (_, request) =>
        {
            if (request.Method == "POST" && request.Url.EndsWith(ContactPage.ApiPath))
                _contactRequests.Add(request);
        };
        // Safety net: block anything but GETs so a test can never send a real message.
        await Context.RouteAsync("**/*", route =>
            route.Request.Method == "GET" ? route.ContinueAsync() : route.AbortAsync());
    }

    // ---------- Positive ----------

    [Test, Category(TestCategories.Positive)]
    public async Task ValidFormPassesValidation()
    {
        await StartPage.FillFormAsync(ValidName, ValidEmail, ValidMessage);

        var (name, email, message) = await StartPage.GetValidityAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(name, Is.True, "Name should be valid");
            Assert.That(email, Is.True, "E-mail should be valid");
            Assert.That(message, Is.True, "Message should be valid");
        }
    }

    [Test, Category(TestCategories.Positive)]
    public async Task ValidFormIsSentToContactApi()
    {
        await StartPage.FillFormAsync(ValidName, ValidEmail, ValidMessage);

        var request = await StartPage.SubmitAndCaptureRequestAsync();

        var body = request.PostDataJSON()!.Value;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Method, Is.EqualTo("POST"));
            Assert.That(body.GetProperty("name").GetString(), Is.EqualTo(ValidName));
            Assert.That(body.GetProperty("email").GetString(), Is.EqualTo(ValidEmail));
            Assert.That(body.GetProperty("message").GetString(), Is.EqualTo(ValidMessage));
            Assert.That(body.GetProperty("website").GetString(), Is.Empty, "Hidden spam-trap field should be blank");
        }
    }

    [TestCase("user@example.com")]
    [TestCase("first.last+tag@sub.example.co.uk")]
    [TestCase("user_name-1@example-domain.com")]
    [Category(TestCategories.Positive)]
    public async Task ValidEmailIsAccepted(string email)
    {
        await StartPage.EmailInput.FillAsync(email);

        Assert.That(await StartPage.IsEmailValidAsync(), Is.True, $"'{email}' should be accepted");
    }

    [Test, Category(TestCategories.Positive)]
    public async Task FieldsAcceptInputUpToMaxLength()
    {
        await StartPage.FillFormAsync(
            new string('a', ContactPage.NameMaxLength),
            EmailOfLength(ContactPage.EmailMaxLength),
            new string('a', ContactPage.MessageMaxLength));

        await Expect(StartPage.NameInput).ToHaveValueAsync(new string('a', ContactPage.NameMaxLength));
        var (name, email, message) = await StartPage.GetValidityAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(name, Is.True, "Name at max length should be valid");
            Assert.That(email, Is.True, "E-mail at max length should be valid");
            Assert.That(message, Is.True, "Message at max length should be valid");
        }
    }

    // ---------- Negative ----------

    [Test, Category(TestCategories.Negative)]
    public async Task EmptySubmitIsBlocked()
    {
        await StartPage.SubmitAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/contact$"));
        var (name, email, message) = await StartPage.GetValidityAsync();
        // Report every field that is wrong, not just the first.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(name, Is.False, "Name should be invalid when empty");
            Assert.That(email, Is.False, "E-mail should be invalid when empty");
            Assert.That(message, Is.False, "Message should be invalid when empty");
            Assert.That(_contactRequests, Is.Empty, "Nothing should be sent");
        }
    }

    [TestCase("", ValidEmail, ValidMessage, TestName = "{m}(missing name)")]
    [TestCase(ValidName, "", ValidMessage, TestName = "{m}(missing e-mail)")]
    [TestCase(ValidName, ValidEmail, "", TestName = "{m}(missing message)")]
    [Category(TestCategories.Negative)]
    public async Task MissingRequiredFieldBlocksSubmit(string name, string email, string message)
    {
        await StartPage.FillFormAsync(name, email, message);
        await StartPage.SubmitAsync();

        var (nameValid, emailValid, messageValid) = await StartPage.GetValidityAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(nameValid, Is.EqualTo(name != ""), "Name validity");
            Assert.That(emailValid, Is.EqualTo(email != ""), "E-mail validity");
            Assert.That(messageValid, Is.EqualTo(message != ""), "Message validity");
            Assert.That(_contactRequests, Is.Empty, "Nothing should be sent");
        }
        await Expect(StartPage.StatusMessage).ToBeEmptyAsync();
    }

    [TestCase("not-an-email")]
    [TestCase("@example.com")]
    [TestCase("user@")]
    [TestCase("user@@example.com")]
    [TestCase("user name@example.com")]
    [Category(TestCategories.Negative)]
    public async Task InvalidEmailIsRejected(string email)
    {
        await StartPage.FillFormAsync(ValidName, email, ValidMessage);
        await StartPage.SubmitAsync();

        await Expect(Page).ToHaveURLAsync(new Regex("/contact$"));
        var (name, emailValid, message) = await StartPage.GetValidityAsync();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(name, Is.True, "Name should be valid");
            Assert.That(emailValid, Is.False, $"E-mail should be invalid for '{email}'");
            Assert.That(message, Is.True, "Message should be valid");
            Assert.That(_contactRequests, Is.Empty, "Nothing should be sent");
        }
    }

    [Test, Category(TestCategories.Negative)]
    public async Task InputBeyondMaxLengthIsTruncated()
    {
        await StartPage.NameInput.FillAsync(new string('a', ContactPage.NameMaxLength + 1));

        await Expect(StartPage.NameInput).ToHaveValueAsync(new string('a', ContactPage.NameMaxLength));
    }

    [Test, Category(TestCategories.Negative)]
    public async Task UnreachableServerShowsError()
    {
        await StartPage.FillFormAsync(ValidName, ValidEmail, ValidMessage);

        // The POST is aborted by the route in SetUp, which looks like a network failure to the page.
        await StartPage.SubmitAndCaptureRequestAsync();

        await Expect(StartPage.StatusMessage).ToHaveTextAsync(new Regex("Could not reach the server"));
    }

    // A syntactically valid e-mail address of exactly the given length.
    private static string EmailOfLength(int length)
    {
        const string domain = "@example.com";
        return new string('a', length - domain.Length) + domain;
    }
}
