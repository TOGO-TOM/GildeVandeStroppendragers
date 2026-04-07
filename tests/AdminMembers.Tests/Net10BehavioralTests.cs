using System.Security.Cryptography;
using System.Text;

namespace AdminMembers.Tests;

/// <summary>
/// Tests validating .NET 10 behavioral changes identified during the upgrade assessment.
/// These verify that the application's usage patterns remain correct after the upgrade.
/// </summary>
[TestClass]
public class Net10BehavioralTests
{
    // ── Uri.EscapeDataString ──────────────────────────────────────────
    // .NET 10 uses stricter RFC 3986 encoding. Verify our URI building still works.

    [TestMethod]
    public void UriEscapeDataString_EncodesSpaces()
    {
        var result = Uri.EscapeDataString("hello world");
        Assert.AreEqual("hello%20world", result);
    }

    [TestMethod]
    public void UriEscapeDataString_EncodesAtSign()
    {
        var result = Uri.EscapeDataString("user@domain.com");
        Assert.AreEqual("user%40domain.com", result);
    }

    [TestMethod]
    public void UriEscapeDataString_EncodesSpecialChars()
    {
        // Characters that might appear in TOTP issuer/username
        var result = Uri.EscapeDataString("Gilde Van De Stroppendragers");
        Assert.AreEqual("Gilde%20Van%20De%20Stroppendragers", result);
    }

    [TestMethod]
    public void UriEscapeDataString_HandlesUnicodeCharacters()
    {
        var result = Uri.EscapeDataString("Ünïcödé");
        // Should be percent-encoded UTF-8 bytes
        Assert.IsFalse(result.Contains('Ü'), "Unicode should be percent-encoded");
        Assert.IsTrue(result.Contains('%'), "Should contain percent-encoded bytes");
        // Round-trip
        Assert.AreEqual("Ünïcödé", Uri.UnescapeDataString(result));
    }

    // ── TimeSpan.From* with int overloads ──────────────────────────────
    // .NET 10 overloads with int parameters — verify our usage is equivalent.

    [TestMethod]
    public void TimeSpanFromMinutes_IntegerValue_MatchesExpected()
    {
        // AuthenticationMiddleware uses TimeSpan.FromMinutes(5)
        var ts = TimeSpan.FromMinutes(5);
        Assert.AreEqual(300, ts.TotalSeconds);
        Assert.AreEqual(5, ts.TotalMinutes);
    }

    [TestMethod]
    public void TimeSpanFromSeconds_IntegerValue_MatchesExpected()
    {
        // Program.cs uses TimeSpan.FromSeconds(value)
        var ts = TimeSpan.FromSeconds(30);
        Assert.AreEqual(30, ts.TotalSeconds);
        Assert.AreEqual(0.5, ts.TotalMinutes);
    }

    [TestMethod]
    public void TimeSpanFromHours_IntegerValue_MatchesExpected()
    {
        var ts = TimeSpan.FromHours(1);
        Assert.AreEqual(3600, ts.TotalSeconds);
        Assert.AreEqual(60, ts.TotalMinutes);
    }

    [TestMethod]
    public void TimeSpanFromMinutes_DoubleValue_StillWorks()
    {
        // Ensure fractional values still work correctly
        var ts = TimeSpan.FromMinutes(1.5);
        Assert.AreEqual(90, ts.TotalSeconds);
    }

    // ── PBKDF2 / Cryptographic operations ─────────────────────────────
    // Verify crypto primitives used by AuthService still work correctly on .NET 10.

    [TestMethod]
    public void Pbkdf2_ProducesConsistentOutput()
    {
        var password = "TestPassword";
        var salt = new byte[32];
        Array.Fill(salt, (byte)0xAB);

        var hash1 = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, 100_000, HashAlgorithmName.SHA512, 64);
        var hash2 = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, 100_000, HashAlgorithmName.SHA512, 64);

        CollectionAssert.AreEqual(hash1, hash2, "Same inputs should produce same PBKDF2 output");
    }

    [TestMethod]
    public void Pbkdf2_DifferentPasswords_ProduceDifferentHashes()
    {
        var salt = new byte[32];
        Array.Fill(salt, (byte)0xCD);

        var hash1 = Rfc2898DeriveBytes.Pbkdf2(
            "Password1", salt, 100_000, HashAlgorithmName.SHA512, 64);
        var hash2 = Rfc2898DeriveBytes.Pbkdf2(
            "Password2", salt, 100_000, HashAlgorithmName.SHA512, 64);

        CollectionAssert.AreNotEqual(hash1, hash2);
    }

    [TestMethod]
    public void CryptographicOperations_FixedTimeEquals_Works()
    {
        // Used in AuthService.VerifyPassword
        var a = new byte[] { 1, 2, 3, 4, 5 };
        var b = new byte[] { 1, 2, 3, 4, 5 };
        var c = new byte[] { 1, 2, 3, 4, 6 };

        Assert.IsTrue(CryptographicOperations.FixedTimeEquals(a, b));
        Assert.IsFalse(CryptographicOperations.FixedTimeEquals(a, c));
    }

    [TestMethod]
    public void SHA256_ComputeHash_WorksCorrectly()
    {
        // Legacy hash support in AuthService.VerifyPassword
        using var sha256 = SHA256.Create();
        var input = Encoding.UTF8.GetBytes("testpassword");
        var hash = sha256.ComputeHash(input);

        Assert.AreEqual(32, hash.Length, "SHA256 should produce 32 bytes");

        // Same input should produce same hash
        var hash2 = sha256.ComputeHash(input);
        CollectionAssert.AreEqual(hash, hash2);
    }

    [TestMethod]
    public void RandomNumberGenerator_GetBytes_ProducesRandomData()
    {
        // Used in AuthService for salt generation and token generation
        var bytes1 = RandomNumberGenerator.GetBytes(32);
        var bytes2 = RandomNumberGenerator.GetBytes(32);

        Assert.AreEqual(32, bytes1.Length);
        Assert.AreEqual(32, bytes2.Length);
        CollectionAssert.AreNotEqual(bytes1, bytes2, "Random bytes should differ");
    }

    // ── ConfigurationBinder.GetValue<T> ───────────────────────────────
    // .NET 10 may throw on invalid config values. Verify our default-value pattern works.

    [TestMethod]
    public void ConfigurationGetValue_WithDefault_ReturnsDefault_WhenKeyMissing()
    {
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var value = config.GetValue<int>("Missing:Key", 42);
        Assert.AreEqual(42, value);
    }

    [TestMethod]
    public void ConfigurationGetValue_WithDefault_ReturnsConfigured_WhenKeyPresent()
    {
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Test:Key", "99" }
            })
            .Build();

        var value = config.GetValue<int>("Test:Key", 42);
        Assert.AreEqual(99, value);
    }

    [TestMethod]
    public void ConfigurationGetValue_BoolDefault_WorksCorrectly()
    {
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var value = config.GetValue<bool>("Missing:Bool", true);
        Assert.IsTrue(value);
    }
}
