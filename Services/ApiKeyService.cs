using System.Security.Cryptography;
using AdminMembers.Data;
using AdminMembers.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AdminMembers.Services
{
    public class ApiKeyService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private const string CachePrefix = "apikey_";
        private const string NegativeCachePrefix = "apikey_neg_";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan NegativeCacheDuration = TimeSpan.FromSeconds(30);

        public ApiKeyService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public (string rawKey, string hash, string prefix) GenerateKey()
        {
            var bytes = RandomNumberGenerator.GetBytes(32); // 256-bit entropy
            var hex = Convert.ToHexStringLower(bytes);
            var rawKey = $"gvds_ak_{hex}";
            var hash = HashKey(rawKey);
            var prefix = rawKey[..12];
            return (rawKey, hash, prefix);
        }

        public static string HashKey(string rawKey)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(rawKey);
            var hashBytes = SHA256.HashData(bytes);
            return Convert.ToHexStringLower(hashBytes);
        }

        public async Task<ApiKey?> ValidateKeyAsync(string rawKey)
        {
            if (string.IsNullOrWhiteSpace(rawKey) || !rawKey.StartsWith("gvds_ak_"))
                return null;

            var hash = HashKey(rawKey);
            var cacheKey = $"{CachePrefix}{hash}";
            var negativeCacheKey = $"{NegativeCachePrefix}{hash}";

            // Check negative cache first (prevents DB hammering with invalid keys)
            if (_cache.TryGetValue(negativeCacheKey, out _))
                return null;

            if (_cache.TryGetValue(cacheKey, out ApiKey? cached))
            {
                // Re-check expiry on cached keys
                if (cached!.ExpiresAt.HasValue && cached.ExpiresAt.Value < DateTime.UtcNow)
                {
                    _cache.Remove(cacheKey);
                    return null;
                }
                return cached;
            }

            var apiKey = await _context.ApiKeys
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.KeyHash == hash && k.IsActive);

            if (apiKey == null)
            {
                _cache.Set(negativeCacheKey, true, NegativeCacheDuration);
                return null;
            }

            if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
            {
                _cache.Set(negativeCacheKey, true, NegativeCacheDuration);
                return null;
            }

            _cache.Set(cacheKey, apiKey, CacheDuration);
            return apiKey;
        }

        public async Task<(ApiKey apiKey, string rawKey)> CreateApiKeyAsync(string name, ApiKeyPermission permission, int userId, DateTime? expiresAt = null)
        {
            var (rawKey, hash, prefix) = GenerateKey();

            var apiKey = new ApiKey
            {
                Name = name,
                KeyHash = hash,
                KeyPrefix = prefix,
                Permission = permission,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                CreatedByUserId = userId
            };

            _context.ApiKeys.Add(apiKey);
            await _context.SaveChangesAsync();

            return (apiKey, rawKey);
        }

        public async Task<bool> RevokeKeyAsync(int id)
        {
            var key = await _context.ApiKeys.FindAsync(id);
            if (key == null) return false;

            key.IsActive = false;
            await _context.SaveChangesAsync();

            // Invalidate both positive and negative caches
            _cache.Remove($"{CachePrefix}{key.KeyHash}");
            _cache.Remove($"{NegativeCachePrefix}{key.KeyHash}");

            return true;
        }

        public async Task<bool> DeleteKeyAsync(int id)
        {
            var key = await _context.ApiKeys.FindAsync(id);
            if (key == null) return false;

            // Invalidate caches
            _cache.Remove($"{CachePrefix}{key.KeyHash}");
            _cache.Remove($"{NegativeCachePrefix}{key.KeyHash}");

            _context.ApiKeys.Remove(key);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<ApiKey>> GetAllKeysAsync()
        {
            return await _context.ApiKeys
                .AsNoTracking()
                .OrderByDescending(k => k.CreatedAt)
                .ToListAsync();
        }
    }
}
