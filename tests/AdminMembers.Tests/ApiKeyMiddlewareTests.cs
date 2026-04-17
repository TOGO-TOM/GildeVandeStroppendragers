using AdminMembers.Middleware;
using System.Reflection;

namespace AdminMembers.Tests;

[TestClass]
public class ApiKeyMiddlewareTests
{
    private static string CallSanitize(string input)
    {
        // Access private static SanitizeHeaderValue via reflection
        var method = typeof(ApiKeyAuthenticationMiddleware)
            .GetMethod("SanitizeHeaderValue", BindingFlags.NonPublic | BindingFlags.Static);
        return (string)method!.Invoke(null, [input])!;
    }

    [TestMethod]
    public void SanitizeHeaderValue_RemovesNewlines()
    {
        var result = CallSanitize("Evil\r\nX-Injected: true");
        Assert.IsFalse(result.Contains('\r'));
        Assert.IsFalse(result.Contains('\n'));
        Assert.AreEqual("EvilX-Injected: true", result);
    }

    [TestMethod]
    public void SanitizeHeaderValue_RemovesControlCharacters()
    {
        var result = CallSanitize("Name\0With\x01Nulls");
        Assert.AreEqual("NameWithNulls", result);
    }

    [TestMethod]
    public void SanitizeHeaderValue_ReturnsUnknownForEmpty()
    {
        Assert.AreEqual("Unknown", CallSanitize(""));
        Assert.AreEqual("Unknown", CallSanitize("\r\n"));
    }

    [TestMethod]
    public void SanitizeHeaderValue_PreservesNormalText()
    {
        Assert.AreEqual("Excel Export Key", CallSanitize("Excel Export Key"));
        Assert.AreEqual("My-Key_123", CallSanitize("My-Key_123"));
    }

    [TestMethod]
    public void SanitizeHeaderValue_RemovesDeleteChar()
    {
        // DEL character (0x7F) should be stripped
        var result = CallSanitize("Test\x7FValue");
        Assert.AreEqual("TestValue", result);
    }
}
