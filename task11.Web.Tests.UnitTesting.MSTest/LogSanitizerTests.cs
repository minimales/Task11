using task11.Web.Infrastructure.Logging;

[TestClass]
public class LogSanitizerTests
{
    [TestMethod]
    public void Test_Sanitize_DuplicateJsonKey_DoesNotThrow_AndReturnsSafeString()
    {
        const string body = "{\"a\":1,\"a\":2}";

        string? result = null;
        Exception? thrown = null;

        try
        {
            result = LogSanitizer.Sanitize(body, maxBodyBytes: 4096);
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

        Assert.IsNull(thrown, "Sanitize must never throw on a duplicate JSON key.");
        Assert.IsNotNull(result, "Sanitize must return a non-null string.");
        Assert.IsTrue(result!.Length > 0, "Sanitize must return a non-empty sanitized/omitted string.");
    }

    [TestMethod]
    public void Test_Sanitize_DuplicateJsonKey_ReturnsOmittedPlaceholder()
    {
        const string body = "{\"a\":1,\"a\":2}";

        string result = LogSanitizer.Sanitize(body, maxBodyBytes: 4096);

        Assert.IsTrue(
            result.StartsWith("[omitted:", StringComparison.Ordinal),
            $"Expected the omitted-placeholder fallback, got: {result}");
    }
}
