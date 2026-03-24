using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    // ReferenceHandler.IgnoreCycles — zapobiega błędom JsonException gdy EF relationship fixup
    // tworzy cykliczne referencje w grafie obiektów (np. Item -> FieldValues -> Item)
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

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
    /// Pobiera wartość z cache lub wykonuje funkcję fabryczną i zapisuje wynik w cache.
    /// Jeśli Redis jest wyłączony (IsEnabled = false) lub niedostępny, zawsze wykonuje factory bez cachowania.
    /// </summary>
    public async Task<T?> GetOrAddAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        if (!settings.IsEnabled || redis == null)
        {
            logger.LogTrace("Redis disabled - executing factory directly for key {Key}", key);
            return await factory();
        }

        IDatabase db = redis.GetDatabase();

        try
        {
            RedisValue cachedValue = await db.StringGetAsync(key);

            if (cachedValue.HasValue)
            {
                try
                {
                    T? result = JsonSerializer.Deserialize<T>(cachedValue.ToString(), JsonOptions);
                    logger.LogDebug("Cache hit for key {Key}", key);
                    return result;
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex, "Failed to deserialize cached value for key {Key}", key);
                    await db.KeyDeleteAsync(key);
                }
            }
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Redis read failed for key {Key}, falling back to factory", key);
            return await factory();
        }

        logger.LogDebug("Cache miss for key {Key}, executing factory", key);
        T value = await factory();

        if (value is not null)
        {
            try
            {
                string serializedValue = JsonSerializer.Serialize(value, JsonOptions);
                await db.StringSetAsync(key, serializedValue, expiration);
                logger.LogDebug("Cached value for key {Key} with expiration {Expiration}", key, expiration);
            }
            catch (RedisException ex)
            {
                logger.LogWarning(ex, "Redis write failed for key {Key}, returning factory result without caching", key);
            }
        }

        return value;
    }

    /// <summary>
    /// Pobiera wiele wartości z cache jednocześnie używając MGET.
    /// Jeśli Redis jest wyłączony lub niedostępny, zwraca pusty słownik.
    /// </summary>
    public async Task<Dictionary<string, T>> GetManyAsync<T>(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default) where T : class
    {
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
        RedisValue[] values;

        try
        {
            values = await db.StringGetAsync(redisKeys);
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Redis MGET failed for {Count} keys, returning empty dictionary", keysList.Count);
            return new Dictionary<string, T>();
        }

        Dictionary<string, T> results = new();

        for (int i = 0; i < keysList.Count; i++)
        {
            if (values[i].HasValue)
            {
                try
                {
                    T? deserialized = JsonSerializer.Deserialize<T>(values[i].ToString(), JsonOptions);
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
    /// Zapisuje wiele wartości w cache używając potoku IBatch.
    /// Każdy klucz jest ustawiany atomicznie z TTL (SET key value PX ms) — eliminuje okno bez TTL po MSET.
    /// Jeśli Redis jest wyłączony lub niedostępny, operacja jest pomijana.
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

        if (!settings.IsEnabled || redis == null)
        {
            logger.LogTrace("Redis disabled - skipping batch SET for {Count} items", items.Count);
            return;
        }

        IDatabase db = redis.GetDatabase();

        try
        {
            // IBatch pipeline: każdy StringSetAsync(key, value, expiry) = atomiczny SET key value PX ms
            // Zamiast MSET (bez TTL) + N×EXPIRE — eliminuje okno w którym klucze nie mają TTL
            IBatch batch = db.CreateBatch();
            List<Task> tasks = new(items.Count);

            foreach (KeyValuePair<string, T> kvp in items)
            {
                string serialized = JsonSerializer.Serialize(kvp.Value, JsonOptions);
                tasks.Add(batch.StringSetAsync((RedisKey)kvp.Key, (RedisValue)serialized, expiration));
            }

            batch.Execute();
            await Task.WhenAll(tasks);

            logger.LogDebug("Cached {Count} values with expiration {Expiration}", items.Count, expiration);
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Redis batch write failed for {Count} items, skipping cache", items.Count);
        }
    }

    /// <summary>
    /// Usuwa pojedynczy klucz z cache.
    /// Jeśli Redis jest wyłączony lub niedostępny, operacja jest pomijana.
    /// </summary>
    public async Task RemoveCacheByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!settings.IsEnabled || redis == null)
        {
            logger.LogTrace("Redis disabled - skipping cache removal for key {Key}", key);
            return;
        }

        try
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
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Redis key delete failed for key {Key}", key);
        }
    }

    /// <summary>
    /// Usuwa wszystkie klucze pasujące do wzorca Redis.
    /// Używa SCAN + batch DEL (zamiast per-key DEL) — minimalizuje round-tripy.
    /// W trybie Redis Cluster skanuje wszystkie węzły primary (nie tylko pierwszy endpoint).
    /// Jeśli Redis jest wyłączony lub niedostępny, operacja jest pomijana.
    /// </summary>
    public async Task RemoveCacheContainsAsync(string pattern, CancellationToken cancellationToken = default)
    {
        if (!settings.IsEnabled || redis == null)
        {
            return;
        }

        EndPoint[] endpoints = redis.GetEndPoints();

        if (endpoints.Length == 0)
        {
            logger.LogWarning("No Redis endpoints available for pattern deletion");
            return;
        }

        IDatabase db = redis.GetDatabase();
        int totalDeleted = 0;

        foreach (EndPoint endpoint in endpoints)
        {
            IServer server = redis.GetServer(endpoint);

            // Pomijamy repliki — SCAN zwraca klucze, ale DEL musi iść przez primary
            if (server.IsReplica)
            {
                continue;
            }

            try
            {
                List<RedisKey> keysToDelete = new();

                await foreach (RedisKey key in server.KeysAsync(database: db.Database, pattern: pattern, pageSize: 250))
                {
                    keysToDelete.Add(key);
                }

                if (keysToDelete.Count > 0)
                {
                    // Batch DEL — jeden round-trip zamiast N×DEL
                    long deleted = await db.KeyDeleteAsync(keysToDelete.ToArray());
                    totalDeleted += (int)deleted;
                }
            }
            catch (RedisException ex)
            {
                logger.LogWarning(ex, "Failed to scan/delete cache entries matching pattern {Pattern} on endpoint {Endpoint}", pattern, endpoint);
            }
        }

        if (totalDeleted > 0)
        {
            logger.LogDebug("Removed {Count} cache entries matching pattern {Pattern}", totalDeleted, pattern);
        }
    }
}
