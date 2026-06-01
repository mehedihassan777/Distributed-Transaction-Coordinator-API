using Application.Common.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace Infrastructure.Caching;

/// <summary>
/// Redis-backed distributed cache implementation.
/// Serialises values as JSON so any type can be cached.
/// TTL defaults to 10 minutes if not specified.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(10);

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(key);
        if (!value.HasValue)
            return default;

        return JsonSerializer.Deserialize<T>(value.ToString());
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value);
        await _database.StringSetAsync(key, json, expiry ?? DefaultExpiry);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => await _database.KeyDeleteAsync(key);
}
