using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Domain.Abstractions.Services;
using Microsoft.Extensions.Caching.Distributed;

namespace Redis;

/// <inheritdoc />
public class RedisService: ICacheService
{
    private readonly IDistributedCache _cache;
    public RedisService(IDistributedCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<T> GetAsync<T>(string key)
    {
        var cached = await _cache.GetStringAsync(key);
        return cached == null ? default : JsonSerializer.Deserialize<T>(cached);
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var serialized = JsonSerializer.Serialize(value);
        var options = new DistributedCacheEntryOptions();
        
        if (expiry.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiry;
        }

        await _cache.SetStringAsync(key, serialized, options);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key)
    {
        var value = await _cache.GetStringAsync(key);
        return value != null;
    }

    /// <inheritdoc />
    public string GenerateCacheKey(
        string prefix,
        long cacheVersion,
        object parameters)
    {
        var json = JsonSerializer.Serialize(parameters);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        var data = Convert.ToHexString(hash).Substring(0,20);
        return $"{prefix}:v{cacheVersion}:{data}";
    }

    /// <inheritdoc />
    public async Task InvalidateCache(string cacheVersionKey)
    {
        var currentVersion = await GetCurrentCacheVersion(cacheVersionKey);
        var newVersion = currentVersion + 1;
        await SetAsync(cacheVersionKey, newVersion);
    }

    /// <inheritdoc />
    public async Task<long> GetCurrentCacheVersion(string cacheVersionKey)
    {
        var version = await GetAsync<long>(cacheVersionKey);
        return version == 0 ? 1 : version;
    }
}