using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Net;
using System.Text.Json;

namespace Business.Implementation.Services;

/// <summary>
/// Implementacja serwisu cache opartego na Redis do przechowywania i pobierania danych tymczasowych
/// Wspiera tryb bez Redis (IsEnabled = false) dla lokalnego developmentu
/// </summary>
public sealed class CacheService : ICacheService
{
    private readonly IConnectionMultiplexer? redis;
    private readonly RedisSettings settings;
    private readonly ILogger<CacheService> logger;

    public CacheService(
        IOptions<RedisSettings> redisSettings,
        ILogger<CacheService> logger,
        IConnectionMultiplexer? redis = null)
    {
        this.settings = redisSettings.Value;
        this.redis = redis;
        this.logger = logger;

        if (!settings.IsEnabled)
        {
            logger.LogWarning("Redis cache is DISABLED - all cache operations will be bypassed and data will be fetched directly from database");
        }
    }

    /// <summary>
    /// Pobiera wartość z cache lub wykonuje funkcję fabryczną i zapisuje wynik w cache
    /// Jeśli Redis jest wyłączony (IsEnabled = false), zawsze wykonuje factory bez cachowania
    /// </summary>
    public async Task<T?> GetOrAddAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        // Jeśli Redis wyłączony - pomijamy cache i zawsze wykonujemy factory
        if (!settings.IsEnabled || redis == null)
        {
            logger.LogTrace("Redis disabled - executing factory directly for key {Key}", key);
            return await factory();
        }

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
    /// Pobiera wiele wartości z cache jednocześnie używając MGET
    /// Jeśli Redis jest wyłączony, zwraca pusty słownik
    /// </summary>
    public async Task<Dictionary<string, T>> GetManyAsync<T>(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default) where T : class
    {
        // Jeśli Redis wyłączony - zwracamy pusty słownik
        if (!settings.IsEnabled || redis == null)
        {
            logger.LogTrace("Redis disabled - returning empty dictionary for MGET");
            return new Dictionary<string, T>();
        }

        IDatabase db = redis.GetDatabase();
        List<string> keysList = keys.ToList();

        if (keysList.Count == 0)
        {
            return new Dictionary<string, T>();
        }

        RedisKey[] redisKeys = keysList.Select(k => (RedisKey)k).ToArray();
        RedisValue[] values = await db.StringGetAsync(redisKeys);

        Dictionary<string, T> results = new Dictionary<string, T>();

        for (int i = 0; i < keysList.Count; i++)
        {
            if (values[i].HasValue)
            {
                try
                {
                    T? deserialized = JsonSerializer.Deserialize<T>(values[i].ToString());
                    if (deserialized is not null)
                    {
                        results[keysList[i]] = deserialized;
                    }
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex, "Failed to deserialize cached value for key {Key}", keysList[i]);
                }
            }
        }

        logger.LogDebug("MGET retrieved {Count}/{Total} values from cache", results.Count, keysList.Count);
        return results;
    }

    /// <summary>
    /// Zapisuje wiele wartości w cache jednocześnie używając MSET
    /// Jeśli Redis jest wyłączony, operacja jest pomijana
    /// </summary>
    public async Task SetManyAsync<T>(
        Dictionary<string, T> items,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        if (items.Count == 0)
        {
            return;
        }

        // Jeśli Redis wyłączony - pomijamy operację
        if (!settings.IsEnabled || redis == null)
        {
            logger.LogTrace("Redis disabled - skipping MSET for {Count} items", items.Count);
            return;
        }

        IDatabase db = redis.GetDatabase();

        KeyValuePair<RedisKey, RedisValue>[] redisKeyValues = items
            .Select(kvp => new KeyValuePair<RedisKey, RedisValue>(
                (RedisKey)kvp.Key,
                (RedisValue)JsonSerializer.Serialize(kvp.Value)))
            .ToArray();

        await db.StringSetAsync(redisKeyValues);

        if (expiration.HasValue)
        {
            foreach (string key in items.Keys)
            {
                await db.KeyExpireAsync((RedisKey)key, expiration.Value);
            }
        }

        logger.LogDebug("MSET cached {Count} values with expiration {Expiration}", items.Count, expiration);
    }

    /// <summary>
    /// Usuwa pojedynczy klucz z cache
    /// Jeśli Redis jest wyłączony, operacja jest pomijana
    /// </summary>
    public async Task RemoveCacheByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        // Jeśli Redis wyłączony - pomijamy operację
        if (!settings.IsEnabled || redis == null)
        {
            logger.LogTrace("Redis disabled - skipping cache removal for key {Key}", key);
            return;
        }

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
    /// Jeśli Redis jest wyłączony, operacja jest pomijana
    /// Używa SCAN zamiast KEYS dla lepszej wydajności w production
    /// </summary>
    public async Task RemoveCacheContainsAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Jeśli Redis wyłączony - pomijamy operację
        if (!settings.IsEnabled || redis == null)
        {
            logger.LogTrace("Redis disabled - skipping pattern cache removal for pattern {Pattern}", pattern);
            return;
        }

        IDatabase db = redis.GetDatabase();
        EndPoint[] endpoints = redis.GetEndPoints();
        
        if (endpoints.Length == 0)
        {
            logger.LogWarning("No Redis endpoints available for pattern deletion");
            return;
        }

        try
        {
            IServer server = redis.GetServer(endpoints[0]);
            
            // Użyj SCAN zamiast KEYS dla lepszej wydajności
            // SCAN nie blokuje Redis i działa w klastrze
            int deletedCount = 0;
            int scannedCount = 0;
            
            await foreach (RedisKey key in server.KeysAsync(database: db.Database, pattern: pattern, pageSize: 250))
            {
                scannedCount++;
                bool deleted = await db.KeyDeleteAsync(key);
                if (deleted)
                {
                    deletedCount++;
                }
            }

            if (scannedCount > 0)
            {
                logger.LogInformation(
                    "Pattern cache removal: scanned {ScannedCount} keys, deleted {DeletedCount} matching pattern {Pattern}",
                    scannedCount, deletedCount, pattern);
            }
            else
            {
                logger.LogDebug("No keys found matching pattern {Pattern}", pattern);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove cache entries matching pattern {Pattern}", pattern);
            throw;
        }
    }
}
