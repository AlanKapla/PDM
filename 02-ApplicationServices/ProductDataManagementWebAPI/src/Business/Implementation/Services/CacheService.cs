using Business.Interfaces.Services;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Net;
using System.Text.Json;

namespace Business.Implementation.Services;

/// <summary>
/// Implementacja serwisu cache opartego na Redis do przechowywania i pobierania danych tymczasowych
/// </summary>
public sealed class CacheService : ICacheService
{
    private readonly IConnectionMultiplexer redis;
    private readonly ILogger<CacheService> logger;

    public CacheService(IConnectionMultiplexer redis, ILogger<CacheService> logger)
    {
        this.redis = redis;
        this.logger = logger;
    }

    /// <summary>
    /// Pobiera wartość z cache lub wykonuje funkcję fabryczną i zapisuje wynik w cache
    /// </summary>
    public async Task<T?> GetOrAddAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        IDatabase db = redis.GetDatabase();

        RedisValue cachedValue = await db.StringGetAsync(key);
        
        if (cachedValue.HasValue)
        {
            try
            {
                T? result = JsonSerializer.Deserialize<T>(cachedValue.ToString());
                logger.LogDebug("Cache hit for key {Key}", key);
                return result;
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Failed to deserialize cached value for key {Key}", key);
                await db.KeyDeleteAsync(key);
            }
        }

        logger.LogDebug("Cache miss for key {Key}, executing factory", key);
        T value = await factory();

        if (value is not null)
        {
            string serializedValue = JsonSerializer.Serialize(value);
            await db.StringSetAsync(key, serializedValue, expiration);
            logger.LogDebug("Cached value for key {Key} with expiration {Expiration}", key, expiration);
        }

        return value;
    }

    /// <summary>
    /// Usuwa pojedynczy klucz z cache
    /// </summary>
    public async Task RemoveCacheByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        IDatabase db = redis.GetDatabase();
        bool deleted = await db.KeyDeleteAsync(key);
        
        if (deleted)
        {
            logger.LogDebug("Removed cache for key {Key}", key);
        }
        else
        {
            logger.LogDebug("Key {Key} not found in cache", key);
        }
    }

    /// <summary>
    /// Usuwa wszystkie klucze pasujące do wzorca Redis
    /// </summary>
    public async Task RemoveCacheContainsAsync(string pattern, CancellationToken cancellationToken = default)
    {
        IDatabase db = redis.GetDatabase();
        EndPoint[] endpoints = redis.GetEndPoints();
        
        if (endpoints.Length == 0)
        {
            logger.LogWarning("No Redis endpoints available for pattern deletion");
            return;
        }

        IServer server = redis.GetServer(endpoints[0]);
        IEnumerable<RedisKey> keys = server.Keys(pattern: pattern);
        
        int deletedCount = 0;
        foreach (RedisKey key in keys)
        {
            bool deleted = await db.KeyDeleteAsync(key);
            if (deleted)
            {
                deletedCount++;
            }
        }

        logger.LogDebug("Removed {Count} cache entries matching pattern {Pattern}", deletedCount, pattern);
    }
}
