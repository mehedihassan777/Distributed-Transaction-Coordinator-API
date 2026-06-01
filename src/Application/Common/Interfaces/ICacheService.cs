namespace Application.Common.Interfaces;

/// <summary>
/// Abstraction over the distributed cache (Redis).
/// Defined in the Application layer to keep cache-logic
/// free of StackExchange.Redis implementation details.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
