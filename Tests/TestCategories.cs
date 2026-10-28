namespace PlaywrightTests.Tests;

// Category names for [Category(...)], so runs can be filtered with --filter "TestCategory=Negative".
public static class TestCategories
{
    // Valid input / happy path: the feature does what it should.
    public const string Positive = "Positive";

    // Invalid input, error paths and edge cases: the feature refuses or fails safely.
    public const string Negative = "Negative";
}
