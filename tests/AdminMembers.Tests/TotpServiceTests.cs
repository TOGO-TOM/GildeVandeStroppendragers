using AdminMembers.Models;
using AdminMembers.Services;
using AdminMembers.Tests.Helpers;

namespace AdminMembers.Tests;

[TestClass]
public class TotpServiceTests
{
    [TestMethod]
    public void GenerateSecret_ReturnsNonEmptyBase32()
    {
        using var context = DbContextFactory.Create();
        var service = new TotpService(context);

        var secret = service.GenerateSecret();

        Assert.IsFalse(string.IsNullOrWhiteSpace(secret));
        // Base32 chars: A-Z, 2-7, =
        Assert.IsTrue(secret.All(c => "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567=".Contains(c)),
            $"Secret should be Base32 encoded: {secret}");
    }

    [TestMethod]
    public void GenerateSecret_ProducesDifferentSecrets()
    {
        using var context = DbContextFactory.Create();
        var service = new TotpService(context);

        var secret1 = service.GenerateSecret();
        var secret2 = service.GenerateSecret();

        Assert.AreNotEqual(secret1, secret2, "Each call should produce a unique secret");
    }

    [TestMethod]
    public void GetOtpUri_ReturnsValidOtpauthUri()
    {
        using var context = DbContextFactory.Create();
        var service = new TotpService(context);

        var secret = service.GenerateSecret();
        var uri = service.GetOtpUri(secret, "testuser");

        Assert.IsTrue(uri.StartsWith("otpauth://totp/"), "Should start with otpauth://totp/");
        StringAssert.Contains(uri, "testuser");
        StringAssert.Contains(uri, $"secret={secret}");
        StringAssert.Contains(uri, "algorithm=SHA1");
        StringAssert.Contains(uri, "digits=6");
        StringAssert.Contains(uri, "period=30");
    }

    [TestMethod]
    public void GetOtpUri_EncodesSpecialCharacters()
    {
        using var context = DbContextFactory.Create();
        var service = new TotpService(context);

        var secret = service.GenerateSecret();
        var uri = service.GetOtpUri(secret, "user with spaces@domain.com");

        // Uri.EscapeDataString should encode spaces as %20 and @ as %40
        StringAssert.Contains(uri, "user%20with%20spaces%40domain.com");
        // The issuer "GildeVanDeStroppendragers" has no special chars, should appear as-is
        StringAssert.Contains(uri, "GildeVanDeStroppendragers");
    }

    [TestMethod]
    public void GetQrCodeDataUri_ReturnsPngDataUri()
    {
        using var context = DbContextFactory.Create();
        var service = new TotpService(context);

        var secret = service.GenerateSecret();
        var otpUri = service.GetOtpUri(secret, "qruser");
        var dataUri = service.GetQrCodeDataUri(otpUri);

        Assert.IsTrue(dataUri.StartsWith("data:image/png;base64,"),
            "Should return a PNG data URI");
        // Verify the base64 portion is valid
        var base64Part = dataUri.Replace("data:image/png;base64,", "");
        var pngBytes = Convert.FromBase64String(base64Part);
        Assert.IsTrue(pngBytes.Length > 0, "PNG data should not be empty");
    }

    [TestMethod]
    public void VerifyCode_EmptyInputs_ReturnsFalse()
    {
        using var context = DbContextFactory.Create();
        var service = new TotpService(context);

        Assert.IsFalse(service.VerifyCode("", "123456"));
        Assert.IsFalse(service.VerifyCode("JBSWY3DPEHPK3PXP", ""));
        Assert.IsFalse(service.VerifyCode("", ""));
    }

    [TestMethod]
    public async Task EnableTotpAsync_SetsSecretAndFlag()
    {
        using var context = DbContextFactory.Create();
        var service = new TotpService(context);

        context.Users.Add(new User
        {
            Username = "totpuser",
            Email = "totp@test.be",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var secret = service.GenerateSecret();
        var result = await service.EnableTotpAsync(1, secret);

        Assert.IsTrue(result);
        var user = await context.Users.FindAsync(1);
        Assert.IsNotNull(user);
        Assert.AreEqual(secret, user.TotpSecret);
        Assert.IsTrue(user.TotpEnabled);
    }

    [TestMethod]
    public async Task DisableTotpAsync_ClearsSecretAndFlag()
    {
        using var context = DbContextFactory.Create();
        var service = new TotpService(context);

        context.Users.Add(new User
        {
            Username = "totpuser2",
            Email = "totp2@test.be",
            PasswordHash = "hash",
            IsActive = true,
            TotpSecret = "EXISTING_SECRET",
            TotpEnabled = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var result = await service.DisableTotpAsync(1);

        Assert.IsTrue(result);
        var user = await context.Users.FindAsync(1);
        Assert.IsNotNull(user);
        Assert.IsNull(user.TotpSecret);
        Assert.IsFalse(user.TotpEnabled);
    }

    [TestMethod]
    public async Task EnableTotpAsync_NonExistentUser_ReturnsFalse()
    {
        using var context = DbContextFactory.Create();
        var service = new TotpService(context);

        var result = await service.EnableTotpAsync(999, "SECRET");
        Assert.IsFalse(result);
    }
}
