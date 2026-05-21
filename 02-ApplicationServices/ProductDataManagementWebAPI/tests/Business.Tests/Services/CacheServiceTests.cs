using Business.Implementation.Services;
using Business.Interfaces.Configurations;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;

namespace Business.Tests.Services;

public class CacheServiceTests
{
    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static CacheService CreateWithRedisDisabled()
    {
        RedisSettings settings = new() { IsEnabled = false, DefaultExpirationMinutes = 60 };
        return new CacheService(Options.Create(settings), NullLogger<CacheService>.Instance, redis: null);
    }

    private sealed class StringWrapper
    {
        public string Value { get; set; } = string.Empty;
    }

    // ─── GetOrAddAsync — Redis disabled ──────────────────────────────────────

    [Fact]
    public async Task GetOrAddAsync_RedisDisabled_AlwaysCallsFactory()
    {
        // Arrange
        CacheService sut = CreateWithRedisDisabled();
        int callCount = 0;

        // Act — two calls
        await sut.GetOrAddAsync("key1", () => { callCount++; return Task.FromResult(new StringWrapper { Value = "v" }); });
        await sut.GetOrAddAsync("key1", () => { callCount++; return Task.FromResult(new StringWrapper { Value = "v2" }); });

        // Assert — factory called both times, no caching
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task GetOrAddAsync_RedisDisabled_ReturnsFactoryValue()
    {
        // Arrange
        CacheService sut = CreateWithRedisDisabled();
        StringWrapper expected = new() { Value = "hello" };

        // Act
        StringWrapper? result = await sut.GetOrAddAsync("k", () => Task.FromResult(expected));

        // Assert
        result.Should().Be(expected);
    }

    // ─── GetManyAsync — Redis disabled ───────────────────────────────────────

    [Fact]
    public async Task GetManyAsync_RedisDisabled_ReturnsEmptyDictionary()
    {
        // Arrange
        CacheService sut = CreateWithRedisDisabled();

        // Act
        Dictionary<string, StringWrapper> result = await sut.GetManyAsync<StringWrapper>(["k1", "k2"]);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetManyAsync_RedisDisabled_EmptyKeys_ReturnsEmptyDictionary()
    {
        // Arrange
        CacheService sut = CreateWithRedisDisabled();

        // Act
        Dictionary<string, StringWrapper> result = await sut.GetManyAsync<StringWrapper>([]);

        // Assert
        result.Should().BeEmpty();
    }

    // ─── SetManyAsync — Redis disabled ────────────────────────────────────────

    [Fact]
    public async Task SetManyAsync_RedisDisabled_DoesNotThrow()
    {
        // Arrange
        CacheService sut = CreateWithRedisDisabled();
        Dictionary<string, StringWrapper> items = new()
        {
            ["k1"] = new StringWrapper { Value = "v1" }
        };

        // Act
        Func<Task> act = () => sut.SetManyAsync(items);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SetManyAsync_EmptyDictionary_DoesNotThrow()
    {
        // Arrange
        CacheService sut = CreateWithRedisDisabled();

        // Act
        Func<Task> act = () => sut.SetManyAsync(new Dictionary<string, StringWrapper>());

        // Assert
        await act.Should().NotThrowAsync();
    }

    // ─── RemoveCacheByKeyAsync — Redis disabled ───────────────────────────────

    [Fact]
    public async Task RemoveCacheByKeyAsync_RedisDisabled_DoesNotThrow()
    {
        // Arrange
        CacheService sut = CreateWithRedisDisabled();

        // Act
        Func<Task> act = () => sut.RemoveCacheByKeyAsync("any-key");

        // Assert
        await act.Should().NotThrowAsync();
    }

    // ─── RemoveCacheContainsAsync — Redis disabled ────────────────────────────

    [Fact]
    public async Task RemoveCacheContainsAsync_RedisDisabled_DoesNotThrow()
    {
        // Arrange
        CacheService sut = CreateWithRedisDisabled();

        // Act
        Func<Task> act = () => sut.RemoveCacheContainsAsync("pattern:*");

        // Assert
        await act.Should().NotThrowAsync();
    }

    // ─── GetOrAddAsync — Redis enabled but null (no connection) ──────────────

    [Fact]
    public async Task GetOrAddAsync_RedisEnabledButNullConnection_CallsFactory()
    {
        // Arrange — IsEnabled=true but redis=null simulates misconfiguration fallback
        RedisSettings settings = new() { IsEnabled = true, DefaultExpirationMinutes = 60 };
        CacheService sut = new(Options.Create(settings), NullLogger<CacheService>.Instance, redis: null);

        int callCount = 0;

        // Act
        StringWrapper? result = await sut.GetOrAddAsync("k",
            () => { callCount++; return Task.FromResult(new StringWrapper { Value = "x" }); });

        // Assert
        callCount.Should().Be(1);
        result!.Value.Should().Be("x");
    }

    // ─── GetOrAddAsync — factory returns null ────────────────────────────────

    [Fact]
    public async Task GetOrAddAsync_RedisDisabled_FactoryReturnsNull_ReturnsNull()
    {
        // Arrange
        CacheService sut = CreateWithRedisDisabled();

        // Act
        StringWrapper? result = await sut.GetOrAddAsync<StringWrapper>("k", () => Task.FromResult<StringWrapper?>(null)!);

        // Assert
        result.Should().BeNull();
    }
}
