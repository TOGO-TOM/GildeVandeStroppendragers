using AdminMembers.Models;
using AdminMembers.Services;
using AdminMembers.Tests.Helpers;
using Microsoft.Extensions.Caching.Memory;

namespace AdminMembers.Tests;

[TestClass]
public class ApiKeyServiceTests
{
    private ApiKeyService CreateService(string? dbName = null)
    {
        var context = DbContextFactory.Create(dbName);
        var cache = new MemoryCache(new MemoryCacheOptions());
        return new ApiKeyService(context, cache);
    }

    private (ApiKeyService service, MemoryCache cache) CreateServiceWithCache(string? dbName = null)
    {
        var context = DbContextFactory.Create(dbName);
        var cache = new MemoryCache(new MemoryCacheOptions());
        return (new ApiKeyService(context, cache), cache);
    }

    [TestMethod]
    public void GenerateKey_Returns256BitEntropy()
    {
        var service = CreateService();
        var (rawKey, hash, prefix) = service.GenerateKey();

        // gvds_ak_ (8 chars) + 64 hex chars (32 bytes) = 72 chars
        Assert.AreEqual(72, rawKey.Length);
        Assert.IsTrue(rawKey.StartsWith("gvds_ak_"));
        Assert.AreEqual(64, hash.Length); // SHA-256 hex
        Assert.AreEqual("gvds_ak_", prefix[..8]); // prefix is first 12 chars
        Assert.AreEqual(12, prefix.Length);
    }

    [TestMethod]
    public void GenerateKey_ProducesUniqueKeys()
    {
        var service = CreateService();
        var keys = Enumerable.Range(0, 100).Select(_ => service.GenerateKey().rawKey).ToList();
        Assert.AreEqual(keys.Count, keys.Distinct().Count());
    }

    [TestMethod]
    public void HashKey_IsConsistent()
    {
        var hash1 = ApiKeyService.HashKey("gvds_ak_test123");
        var hash2 = ApiKeyService.HashKey("gvds_ak_test123");
        Assert.AreEqual(hash1, hash2);
    }

    [TestMethod]
    public void HashKey_DifferentInputsDifferentOutput()
    {
        var hash1 = ApiKeyService.HashKey("gvds_ak_aaa");
        var hash2 = ApiKeyService.HashKey("gvds_ak_bbb");
        Assert.AreNotEqual(hash1, hash2);
    }

    [TestMethod]
    public async Task CreateApiKey_StoresHashedKey()
    {
        var db = "create_test";
        var service = CreateService(db);

        var (apiKey, rawKey) = await service.CreateApiKeyAsync("Test Key", ApiKeyPermission.Read, 1);

        Assert.IsTrue(apiKey.Id > 0);
        Assert.AreEqual("Test Key", apiKey.Name);
        Assert.AreEqual(ApiKeyPermission.Read, apiKey.Permission);
        Assert.IsTrue(apiKey.IsActive);
        Assert.IsTrue(rawKey.StartsWith("gvds_ak_"));
        // KeyHash should be SHA-256 of rawKey, not the raw key itself
        Assert.AreEqual(ApiKeyService.HashKey(rawKey), apiKey.KeyHash);
        Assert.AreNotEqual(rawKey, apiKey.KeyHash);
    }

    [TestMethod]
    public async Task CreateApiKey_AppliesExpiresAt()
    {
        var service = CreateService();
        var expiry = DateTime.UtcNow.AddDays(30);

        var (apiKey, _) = await service.CreateApiKeyAsync("Expiring Key", ApiKeyPermission.Read, 1, expiry);

        Assert.IsNotNull(apiKey.ExpiresAt);
        Assert.AreEqual(expiry, apiKey.ExpiresAt.Value);
    }

    [TestMethod]
    public async Task ValidateKey_ReturnsKeyForValidKey()
    {
        var db = "validate_valid";
        var service = CreateService(db);

        var (_, rawKey) = await service.CreateApiKeyAsync("Valid Key", ApiKeyPermission.ReadWrite, 1);
        var result = await service.ValidateKeyAsync(rawKey);

        Assert.IsNotNull(result);
        Assert.AreEqual("Valid Key", result.Name);
        Assert.AreEqual(ApiKeyPermission.ReadWrite, result.Permission);
    }

    [TestMethod]
    public async Task ValidateKey_ReturnsNullForInvalidKey()
    {
        var service = CreateService();
        var result = await service.ValidateKeyAsync("gvds_ak_0000000000000000000000000000000000000000000000000000000000000000");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task ValidateKey_ReturnsNullForWrongPrefix()
    {
        var service = CreateService();
        var result = await service.ValidateKeyAsync("wrong_prefix_abc123");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task ValidateKey_ReturnsNullForEmptyString()
    {
        var service = CreateService();
        Assert.IsNull(await service.ValidateKeyAsync(""));
        Assert.IsNull(await service.ValidateKeyAsync("   "));
    }

    [TestMethod]
    public async Task ValidateKey_ReturnsNullForExpiredKey()
    {
        var db = "validate_expired";
        var service = CreateService(db);

        var expiry = DateTime.UtcNow.AddMinutes(-1); // Already expired
        var (_, rawKey) = await service.CreateApiKeyAsync("Expired Key", ApiKeyPermission.Read, 1, expiry);
        var result = await service.ValidateKeyAsync(rawKey);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task RevokeKey_InvalidatesKey()
    {
        var db = "revoke_test";
        var service = CreateService(db);

        var (apiKey, rawKey) = await service.CreateApiKeyAsync("Revoke Me", ApiKeyPermission.Read, 1);

        // Validate first to populate cache
        var valid = await service.ValidateKeyAsync(rawKey);
        Assert.IsNotNull(valid);

        // Revoke
        var revoked = await service.RevokeKeyAsync(apiKey.Id);
        Assert.IsTrue(revoked);

        // Should no longer validate
        var afterRevoke = await service.ValidateKeyAsync(rawKey);
        Assert.IsNull(afterRevoke);
    }

    [TestMethod]
    public async Task RevokeKey_ReturnsFalseForNonexistent()
    {
        var service = CreateService();
        var result = await service.RevokeKeyAsync(99999);
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task ValidateKey_CachesResult()
    {
        var db = "cache_test";
        var (service, cache) = CreateServiceWithCache(db);

        var (_, rawKey) = await service.CreateApiKeyAsync("Cached Key", ApiKeyPermission.Read, 1);
        var hash = ApiKeyService.HashKey(rawKey);

        // First call hits DB
        var result1 = await service.ValidateKeyAsync(rawKey);
        Assert.IsNotNull(result1);

        // Verify it's cached
        Assert.IsTrue(cache.TryGetValue($"apikey_{hash}", out _));

        // Second call should use cache
        var result2 = await service.ValidateKeyAsync(rawKey);
        Assert.IsNotNull(result2);
        Assert.AreEqual(result1!.Id, result2!.Id);
    }

    [TestMethod]
    public async Task ValidateKey_NegativeCachePreventsDatabaseHammering()
    {
        var db = "neg_cache_test";
        var (service, cache) = CreateServiceWithCache(db);
        var fakeKey = "gvds_ak_aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        var hash = ApiKeyService.HashKey(fakeKey);

        // First call — hits DB, gets null, caches negative
        var result1 = await service.ValidateKeyAsync(fakeKey);
        Assert.IsNull(result1);
        Assert.IsTrue(cache.TryGetValue($"apikey_neg_{hash}", out _));

        // Second call — hits negative cache, skips DB
        var result2 = await service.ValidateKeyAsync(fakeKey);
        Assert.IsNull(result2);
    }

    [TestMethod]
    public async Task GetAllKeys_ReturnsAllKeys()
    {
        var db = "getall_test";
        var service = CreateService(db);

        await service.CreateApiKeyAsync("Key 1", ApiKeyPermission.Read, 1);
        await service.CreateApiKeyAsync("Key 2", ApiKeyPermission.ReadWrite, 1);

        var all = await service.GetAllKeysAsync();
        Assert.AreEqual(2, all.Count);
    }
}
