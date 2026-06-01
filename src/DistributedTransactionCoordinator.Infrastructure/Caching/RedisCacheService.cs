using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using DistributedTransactionCoordinator.Application.Common.Interfaces;

namespace DistributedTransactionCoordinator.Infrastructure.Caching;

/// <summary>
/// Redis-backed distributed cache implementation.
/// Uses IDistributedCache abstraction so it can be swapped in tests with an in-memory implementation.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public RedisCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var bytes = await _cache.GetAsync(key, cancellationToken);
        if (bytes is null || bytes.Length == 0) return default;
        return JsonSerializer.Deserialize<T>(bytes);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(10)
        };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        await _cache.SetAsync(key, bytes, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => await _cache.RemoveAsync(key, cancellationToken);
}
