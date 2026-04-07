using AdminMembers.Services;
using Microsoft.Extensions.Configuration;

namespace AdminMembers.Tests;

[TestClass]
public class PasswordPolicyServiceTests
{
    private static PasswordPolicyService CreateService(Dictionary<string, string?>? settings = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(settings ?? new Dictionary<string, string?>())
            .Build();
        return new PasswordPolicyService(config);
    }

    [TestMethod]
    public void Validate_StrongPassword_WithDefaults_Succeeds()
    {
        var service = CreateService();
        var (valid, message) = service.Validate("MyStr0ng@Pass!");
        Assert.IsTrue(valid, $"Strong password should pass: {message}");
    }

    [TestMethod]
    public void Validate_TooShort_Fails()
    {
        var service = CreateService();
        var (valid, message) = service.Validate("Ab1!");
        Assert.IsFalse(valid);
        StringAssert.Contains(message, "at least");
    }

    [TestMethod]
    public void Validate_NoUppercase_Fails()
    {
        var service = CreateService();
        var (valid, message) = service.Validate("mystrongpass1!");
        Assert.IsFalse(valid);
        StringAssert.Contains(message, "uppercase");
    }

    [TestMethod]
    public void Validate_NoLowercase_Fails()
    {
        var service = CreateService();
        var (valid, message) = service.Validate("MYSTRONGPASS1!");
        Assert.IsFalse(valid);
        StringAssert.Contains(message, "lowercase");
    }

    [TestMethod]
    public void Validate_NoDigit_Fails()
    {
        var service = CreateService();
        var (valid, message) = service.Validate("MyStrongPass!");
        Assert.IsFalse(valid);
        StringAssert.Contains(message, "digit");
    }

    [TestMethod]
    public void Validate_NoSpecialChar_Fails()
    {
        var service = CreateService();
        var (valid, message) = service.Validate("MyStrongPass1");
        Assert.IsFalse(valid);
        StringAssert.Contains(message, "special");
    }

    [TestMethod]
    public void Validate_CustomMinLength_Enforced()
    {
        var service = CreateService(new Dictionary<string, string?>
        {
            { "PasswordPolicy:MinLength", "12" }
        });

        var (valid1, _) = service.Validate("Short1!aB");   // 9 chars
        Assert.IsFalse(valid1, "Should fail with 9 chars when min is 12");

        var (valid2, _) = service.Validate("LongEnough1!x"); // 13 chars
        Assert.IsTrue(valid2, "Should pass with 13 chars when min is 12");
    }

    [TestMethod]
    public void Validate_DisabledRequirements_AreRespected()
    {
        var service = CreateService(new Dictionary<string, string?>
        {
            { "PasswordPolicy:RequireUppercase", "false" },
            { "PasswordPolicy:RequireSpecialChar", "false" }
        });

        var (valid, message) = service.Validate("lowercase1only");
        Assert.IsTrue(valid, $"Should pass without uppercase/special: {message}");
    }

    [TestMethod]
    public void GetRequirementsHint_ContainsDefaultRequirements()
    {
        var service = CreateService();
        var hint = service.GetRequirementsHint();

        StringAssert.Contains(hint, "8 characters");
        StringAssert.Contains(hint, "uppercase");
        StringAssert.Contains(hint, "lowercase");
        StringAssert.Contains(hint, "digit");
        StringAssert.Contains(hint, "special");
    }

    [TestMethod]
    public void GetRequirementsHint_ReflectsCustomConfig()
    {
        var service = CreateService(new Dictionary<string, string?>
        {
            { "PasswordPolicy:MinLength", "16" },
            { "PasswordPolicy:RequireDigit", "false" }
        });

        var hint = service.GetRequirementsHint();
        StringAssert.Contains(hint, "16 characters");
        // digit should NOT appear
        Assert.IsFalse(hint.Contains("digit", StringComparison.OrdinalIgnoreCase),
            "Hint should not mention digit when disabled");
    }

    [TestMethod]
    public void Validate_EmptyConfig_UsesDefaults()
    {
        // No config at all — should use default min=8, all requirements true
        var service = CreateService();

        var (valid, _) = service.Validate("MyStr0ng@Pass!");
        Assert.IsTrue(valid, "Should use default policy when config is empty");
    }
}
