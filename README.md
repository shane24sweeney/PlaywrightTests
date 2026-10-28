# PlaywrightTests

End-to-end tests for https://selenium-automation.com, written with [Playwright for .NET](https://playwright.dev/dotnet/) and NUnit.

## Prerequisites

- .NET 10 SDK
- PowerShell (`brew install powershell`), used to run the Playwright CLI
- Playwright browsers. Build once, then install them:

  ```bash
  dotnet build
  pwsh bin/Debug/net10.0/playwright.ps1 install
  ```

  Re-run the install command after upgrading the `Microsoft.Playwright.NUnit` package. If a test fails with "Executable doesn't exist", this is the fix.

## Running the tests

```bash
dotnet test
```

The project picks up [.runsettings](.runsettings) automatically, for both `dotnet test` and the VS Code Test Explorer. By default it runs Chromium in a visible (headed) window with `SlowMo` enabled so you can watch each step.

Any setting in `.runsettings` can be overridden for a single run by passing it after `--`:

```bash
dotnet test -- Playwright.<Setting>=<value>
```

### Headless or headed

| Mode | Command |
| --- | --- |
| Headed (default, browser window visible) | `dotnet test` |
| Headless (no window, fastest) | `dotnet test -- Playwright.LaunchOptions.Headless=true Playwright.LaunchOptions.SlowMo=0` |

To change the default, edit `.runsettings`:

```xml
<LaunchOptions>
  <Headless>true</Headless>   <!-- false = show the browser window -->
  <SlowMo>500</SlowMo>        <!-- ms delay between actions; 0 or remove to run at full speed -->
</LaunchOptions>
```

### Different browsers

Supported values are `chromium` (default), `firefox` and `webkit` (Safari's engine).

```bash
dotnet test -- Playwright.BrowserName=firefox
dotnet test -- Playwright.BrowserName=webkit
```

Combine with headless for a quick cross-browser check:

```bash
for b in chromium firefox webkit; do
  dotnet test -- Playwright.BrowserName=$b Playwright.LaunchOptions.Headless=true Playwright.LaunchOptions.SlowMo=0
done
```

To change the default browser, set `<BrowserName>` in `.runsettings`.

To use installed Google Chrome or Microsoft Edge instead of Playwright's bundled Chromium:

```bash
dotnet test -- Playwright.BrowserName=chromium Playwright.LaunchOptions.Channel=chrome
```

### Parallel execution

Every test runs in parallel, including the separate cases of one `[TestCase]` test. The setup is in [Tests/ParallelConfig.cs](Tests/ParallelConfig.cs):

- `Parallelizable(ParallelScope.All)` lets any test run alongside any other.
- `FixtureLifeCycle(InstancePerTestCase)` gives each test its own fixture instance, so its `Page`, `Context` and `StartPage` are never shared.

Each test also gets its own browser context, so cookies and storage never leak between tests.

The number of tests running at once is set in `.runsettings`:

```xml
<NUnit>
  <NumberOfTestWorkers>4</NumberOfTestWorkers>
</NUnit>
```

Override it for a single run:

```bash
dotnet test -- NUnit.NumberOfTestWorkers=8   # more parallel
dotnet test -- NUnit.NumberOfTestWorkers=1   # one test at a time (handy when watching headed runs)
```

When writing tests, don't use `static` fields for test state. Static state is shared across threads, so parallel tests would interfere with each other.

### Running a subset of tests

```bash
dotnet test --filter "FullyQualifiedName~HomePageTests"                # one class
dotnet test --filter "FullyQualifiedName~HomePageTests.ShowsNameAndSummary"  # one test
dotnet test --filter "Name~NavLinkOpensPage"                           # all cases of a parameterised test
dotnet test --filter "TestCategory=Positive"                           # happy-path tests only
dotnet test --filter "TestCategory=Negative"                           # invalid input / error-path tests only
```

### Positive and negative tests

Every test is tagged with a category from [Tests/TestCategories.cs](Tests/TestCategories.cs):

- **Positive:** valid input or normal use, where the feature does what it should. Examples: nav links open the right page, a valid contact form is sent to `/api/contact` with the right data, and fields accept input up to their max length.
- **Negative:** invalid input, error paths and edge cases, where the feature refuses or fails safely. Examples: empty, missing or invalid fields block the submit and nothing is sent, input over the max length is cut off, an unreachable server shows an error, unknown URLs redirect to `/home`, and only one nav link is marked current.

Tag new tests with `[Category(TestCategories.Positive)]` or `[Category(TestCategories.Negative)]`.

The contact tests never send a real message. Every non-GET request is aborted, and the tests assert on the request the page *tried* to send.

## Tracing

Every test records a Playwright trace (actions, screenshots, DOM snapshots, network, console and source). Traces are **kept only for failing tests**, so passing runs leave nothing behind.

Failed-test traces are saved to:

```
bin/Debug/net10.0/playwright-traces/<Namespace.Class.Test>.zip
```

They are also attached to the test result, so the path shows up in the test output.

Open a trace in the Trace Viewer:

```bash
pwsh bin/Debug/net10.0/playwright.ps1 show-trace bin/Debug/net10.0/playwright-traces/<TestName>.zip
```

Or drag the zip onto https://trace.playwright.dev (it runs entirely in your browser; nothing is uploaded).

Tracing is set up in `StartTracing` / `StopTracing` in [Tests/SiteTest.cs](Tests/SiteTest.cs).

### Stepping through a test live

To pause and step through a test interactively with the Playwright Inspector:

```bash
PWDEBUG=1 dotnet test --filter "FullyQualifiedName~ContactPageTests.InvalidEmailIsRejected"
```

## Test fixtures

Tests are built from a small chain of base classes in [Tests/](Tests/):

| Class | What it gives you |
| --- | --- |
| `PageTest` (from Playwright) | A fresh browser context and `Page` per test, plus `Expect(...)` assertions. |
| `SiteTest` | Sets `BaseURL` to `https://selenium-automation.com` so page objects use relative paths, and handles tracing. |
| `SitePageTest<TPage>` | Creates the page object `TPage` and navigates to it before each test, exposed as `StartPage`. |

### Page objects

The framework follows the Page Object pattern. Page objects live in [Pages/](Pages/):

| Class | Role |
| --- | --- |
| `BasePage` | Shared layout on every page: `Nav` (a `NavBar` component), `Heading` and `GotoAsync()`. Each page sets its own `Path`. |
| `HomePage`, `ContactPage` | One class per page: locators as properties and user actions as methods. |
| `SitePage` | Any other page, for tests that only check the shared layout. |
| `Components/NavBar` | The primary navigation, reused by every page. |

Conventions:

- **Tests never use selectors.** They go through page-object properties and methods.
- **Assertions live in tests, not page objects.** Page objects expose locators so tests can use Playwright's auto-retrying `Expect(...)`.
- **Actions that leave the page return the next page object**, e.g. `var contact = await home.ClickContactMeAsync();` or `await home.Nav.GoToContactAsync()`.
- **Page-specific queries are instance methods**, e.g. `contact.IsEmailValidAsync()`.
- **Sections shared across pages are components** under `Pages/Components/`.

### Writing a test class

Inherit from `SitePageTest<TPage>` with the page the tests start on, and say how to create it:

```csharp
public class HomePageTests : SitePageTest<HomePage>
{
    protected override HomePage CreatePage(IPage page) => new(page);

    [Test]
    public async Task ShowsNameAndSummary()
    {
        // StartPage is already open on /home.
        await Expect(StartPage.Heading).ToHaveTextAsync("Shane James Sweeney");
    }
}
```

Every test must assert something. Use Playwright's `Expect(...)` for anything on the page, since it retries until the page catches up. Use NUnit's `Assert.That(...)` for plain values. When checking several independent values, wrap them in `using (Assert.EnterMultipleScope()) { ... }` so a failure reports all of them, not just the first. Give each assertion a message saying what was expected.

Use `[TestCase(...)]` for parameterised tests (see [Tests/NavigationTests.cs](Tests/NavigationTests.cs)).

Extra per-test setup goes in an NUnit `[SetUp]` method in the test class. It runs after the base fixtures, so `Page`, `Context` and `StartPage` are ready to use. For example, [Tests/ContactPageTests.cs](Tests/ContactPageTests.cs) blocks every non-GET request so a test can never send a real message.

For a test that shouldn't open a page automatically, inherit from `SiteTest` instead and navigate yourself.
