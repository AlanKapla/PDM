using Business.Implementation.CacheKeys;
using Business.Implementation.Services;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.WorkSchedules;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Business.Tests.Services;

public class WorkScheduleCacheServiceTests
{
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly WorkScheduleCacheService _sut;

    public WorkScheduleCacheServiceTests()
    {
        _sut = new WorkScheduleCacheService(_cacheMock.Object, NullLogger<WorkScheduleCacheService>.Instance);
    }

    // ─── GetOrBuildScheduleAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetOrBuildScheduleAsync_CallsCacheWithCorrectKey()
    {
        // Arrange
        Guid scheduleId = Guid.NewGuid();
        WorkScheduleDetailsWeb expected = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "Test", DateTime.UtcNow, Guid.NewGuid(), "User", [], []);
        Func<Task<WorkScheduleDetailsWeb>> factory = () => Task.FromResult(expected);
        string expectedKey = WorkScheduleCacheKeys.Schedule(scheduleId);

        _cacheMock
            .Setup(c => c.GetOrAddAsync(
                expectedKey,
                It.IsAny<Func<Task<WorkScheduleDetailsWeb>>>(),
                WorkScheduleCacheKeys.Ttl,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        WorkScheduleDetailsWeb? result = await _sut.GetOrBuildScheduleAsync(scheduleId, factory);

        // Assert
        result.Should().Be(expected);
        _cacheMock.Verify(c => c.GetOrAddAsync(
            expectedKey,
            It.IsAny<Func<Task<WorkScheduleDetailsWeb>>>(),
            WorkScheduleCacheKeys.Ttl,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOrBuildScheduleAsync_WhenCacheReturnsNull_ReturnsNull()
    {
        // Arrange
        Guid scheduleId = Guid.NewGuid();

        _cacheMock
            .Setup(c => c.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<WorkScheduleDetailsWeb>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkScheduleDetailsWeb?)null);

        // Act
        WorkScheduleDetailsWeb dummy = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "T", DateTime.UtcNow, Guid.NewGuid(), "U", [], []);
        WorkScheduleDetailsWeb? result = await _sut.GetOrBuildScheduleAsync(
            scheduleId, () => Task.FromResult(dummy));

        // Assert
        result.Should().BeNull();
    }

    // ─── InvalidateScheduleAsync ──────────────────────────────────────────────

    [Fact]
    public async Task InvalidateScheduleAsync_CallsRemoveCacheContainsWithPattern()
    {
        // Arrange
        Guid scheduleId = Guid.NewGuid();
        string expectedPattern = WorkScheduleCacheKeys.SchedulePattern(scheduleId);

        _cacheMock
            .Setup(c => c.RemoveCacheContainsAsync(expectedPattern, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.InvalidateScheduleAsync(scheduleId);

        // Assert
        _cacheMock.Verify(c => c.RemoveCacheContainsAsync(expectedPattern, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── InvalidateWorkAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task InvalidateWorkAsync_RemovesBothScheduleKeyAndWorkPattern()
    {
        // Arrange
        Guid scheduleId = Guid.NewGuid();
        Guid workId = Guid.NewGuid();
        string expectedScheduleKey = WorkScheduleCacheKeys.Schedule(scheduleId);
        string expectedWorkPattern = WorkScheduleCacheKeys.WorkPattern(scheduleId, workId);

        _cacheMock
            .Setup(c => c.RemoveCacheByKeyAsync(expectedScheduleKey, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _cacheMock
            .Setup(c => c.RemoveCacheContainsAsync(expectedWorkPattern, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.InvalidateWorkAsync(scheduleId, workId);

        // Assert
        _cacheMock.Verify(c => c.RemoveCacheByKeyAsync(expectedScheduleKey, It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveCacheContainsAsync(expectedWorkPattern, It.IsAny<CancellationToken>()), Times.Once);
    }
}
