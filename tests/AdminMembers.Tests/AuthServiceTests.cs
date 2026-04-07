using AdminMembers.Data;
using AdminMembers.Models;
using AdminMembers.Services;
using AdminMembers.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AdminMembers.Tests;

[TestClass]
public class AuthServiceTests
{
    private static AuthService CreateService(ApplicationDbContext context, IConfiguration? config = null)
    {
        config ??= new ConfigurationBuilder().Build();
        var logger = NullLogger<AuthService>.Instance;
        var auditLogger = NullLogger<AuditLogService>.Instance;
        var auditService = new AuditLogService(context, auditLogger);
        var passwordPolicy = new PasswordPolicyService(config);
        return new AuthService(context, logger, auditService, passwordPolicy);
    }

    [TestMethod]
    public void HashPassword_ProducesPbkdf2Format()
    {
        using var context = DbContextFactory.Create();
        var service = CreateService(context);

        var hash = service.HashPasswordPublic("Test@1234");

        Assert.IsTrue(hash.StartsWith("pbkdf2$"), "Hash should use PBKDF2 format");
        var parts = hash.Split('$');
        Assert.AreEqual(3, parts.Length, "Hash should have 3 parts: prefix, salt, hash");
    }

    [TestMethod]
    public void HashPassword_DifferentSaltsEachTime()
    {
        using var context = DbContextFactory.Create();
        var service = CreateService(context);

        var hash1 = service.HashPasswordPublic("Test@1234");
        var hash2 = service.HashPasswordPublic("Test@1234");

        Assert.AreNotEqual(hash1, hash2, "Same password should produce different hashes (different salts)");
    }

    [TestMethod]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        using var context = DbContextFactory.Create();
        var service = CreateService(context);

        var hash = service.HashPasswordPublic("MySecure@Pass1");
        var result = service.VerifyPasswordPublic("MySecure@Pass1", hash);

        Assert.IsTrue(result, "Correct password should verify successfully");
    }

    [TestMethod]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        using var context = DbContextFactory.Create();
        var service = CreateService(context);

        var hash = service.HashPasswordPublic("MySecure@Pass1");
        var result = service.VerifyPasswordPublic("WrongPassword1!", hash);

        Assert.IsFalse(result, "Wrong password should fail verification");
    }

    [TestMethod]
    public async Task RegisterUser_Success()
    {
        using var context = DbContextFactory.Create();
        var service = CreateService(context);

        // Seed a role
        context.Roles.Add(new Role { Name = "User", Permission = Permission.Read });
        await context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "newuser@test.be",
            Password = "Str0ng@Pass!",
            RoleIds = new List<int> { 1 }
        };

        var (success, message, user) = await service.RegisterUserAsync(request, 1, "admin", "127.0.0.1");

        Assert.IsTrue(success, $"Registration should succeed: {message}");
        Assert.IsNotNull(user);
        Assert.AreEqual("newuser", user.Username);
        Assert.IsTrue(user.IsActive);
    }

    [TestMethod]
    public async Task RegisterUser_DuplicateUsername_Fails()
    {
        using var context = DbContextFactory.Create();
        var service = CreateService(context);

        // Seed existing user
        context.Users.Add(new User
        {
            Username = "existing",
            Email = "existing@test.be",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Username = "existing",
            Email = "new@test.be",
            Password = "Str0ng@Pass!",
            RoleIds = new List<int>()
        };

        var (success, message, _) = await service.RegisterUserAsync(request, 1, "admin", "127.0.0.1");

        Assert.IsFalse(success);
        Assert.AreEqual("Username already exists", message);
    }

    [TestMethod]
    public async Task RegisterUser_WeakPassword_Fails()
    {
        using var context = DbContextFactory.Create();
        var service = CreateService(context);

        var request = new RegisterRequest
        {
            Username = "weakuser",
            Email = "weak@test.be",
            Password = "short",
            RoleIds = new List<int>()
        };

        var (success, message, _) = await service.RegisterUserAsync(request, 1, "admin", "127.0.0.1");

        Assert.IsFalse(success);
        StringAssert.Contains(message, "Password must");
    }

    [TestMethod]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
    {
        using var context = DbContextFactory.Create();
        var service = CreateService(context);

        // Seed a user
        var passwordHash = service.HashPasswordPublic("Login@Test1");
        context.Users.Add(new User
        {
            Username = "loginuser",
            Email = "login@test.be",
            PasswordHash = passwordHash,
            IsActive = true,
            IsApproved = true,
            CreatedAt = DateTime.UtcNow
        });
        context.Roles.Add(new Role { Name = "User", Permission = Permission.Read });
        await context.SaveChangesAsync();
        context.UserRoles.Add(new UserRole { UserId = 1, RoleId = 1 });
        await context.SaveChangesAsync();

        var result = await service.LoginAsync(
            new LoginRequest { Username = "loginuser", Password = "Login@Test1" },
            "127.0.0.1");

        Assert.IsTrue(result.Success, $"Login should succeed: {result.Message}");
        Assert.IsNotNull(result.Token);
        Assert.IsNotNull(result.User);
        Assert.AreEqual("loginuser", result.User.Username);
    }

    [TestMethod]
    public async Task LoginAsync_InvalidPassword_ReturnsFail()
    {
        using var context = DbContextFactory.Create();
        var service = CreateService(context);

        context.Users.Add(new User
        {
            Username = "user1",
            Email = "user1@test.be",
            PasswordHash = service.HashPasswordPublic("Correct@Pass1"),
            IsActive = true,
            IsApproved = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var result = await service.LoginAsync(
            new LoginRequest { Username = "user1", Password = "Wrong@Pass1" },
            "127.0.0.1");

        Assert.IsFalse(result.Success);
        Assert.AreEqual("Invalid username or password", result.Message);
    }

    [TestMethod]
    public async Task LoginAsync_InactiveUser_ReturnsFail()
    {
        using var context = DbContextFactory.Create();
        var service = CreateService(context);

        context.Users.Add(new User
        {
            Username = "inactive",
            Email = "inactive@test.be",
            PasswordHash = service.HashPasswordPublic("Test@1234"),
            IsActive = false,
            IsApproved = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var result = await service.LoginAsync(
            new LoginRequest { Username = "inactive", Password = "Test@1234" },
            "127.0.0.1");

        Assert.IsFalse(result.Success);
    }

    [TestMethod]
    public async Task LoginAsync_UnapprovedUser_ReturnsPendingMessage()
    {
        using var context = DbContextFactory.Create();
        var service = CreateService(context);

        context.Users.Add(new User
        {
            Username = "pending",
            Email = "pending@test.be",
            PasswordHash = service.HashPasswordPublic("Test@1234"),
            IsActive = true,
            IsApproved = false,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var result = await service.LoginAsync(
            new LoginRequest { Username = "pending", Password = "Test@1234" },
            "127.0.0.1");

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Message, "pending");
    }

    [TestMethod]
    public async Task GeneratePasswordResetToken_ValidEmail_ReturnsToken()
    {
        using var context = DbContextFactory.Create();
        var service = CreateService(context);

        context.Users.Add(new User
        {
            Username = "resetuser",
            Email = "reset@test.be",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var (success, token) = await service.GeneratePasswordResetTokenAsync("reset@test.be");

        Assert.IsTrue(success);
        Assert.IsFalse(string.IsNullOrEmpty(token));
    }

    [TestMethod]
    public async Task GeneratePasswordResetToken_InvalidEmail_ReturnsFalse()
    {
        using var context = DbContextFactory.Create();
        var service = CreateService(context);

        var (success, _) = await service.GeneratePasswordResetTokenAsync("nobody@test.be");

        Assert.IsFalse(success);
    }

    [TestMethod]
    public async Task DeactivateUser_SetsIsActiveFalse()
    {
        using var context = DbContextFactory.Create();
        var service = CreateService(context);

        context.Users.Add(new User
        {
            Username = "toDeactivate",
            Email = "deact@test.be",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var result = await service.DeactivateUserAsync(1, 1, "admin", "127.0.0.1");

        Assert.IsTrue(result);
        var user = await service.GetRawUserByIdAsync(1);
        Assert.IsNotNull(user);
        Assert.IsFalse(user.IsActive);
    }
}
