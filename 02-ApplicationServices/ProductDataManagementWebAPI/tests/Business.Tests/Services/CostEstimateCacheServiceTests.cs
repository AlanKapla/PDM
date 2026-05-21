using Business.Implementation.CacheKeys;
using Business.Implementation.Services;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Business.Tests.Services;

public class CostEstimateCacheServiceTests
{
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly Mock<Repositories.Repository.Interfaces.IReadRepository<CostEstimate>> _ceRepoMock = new();
    private readonly Mock<Repositories.Repository.Interfaces.IReadRepository<CostEstimateTemplate>> _templateRepoMock = new();
    private readonly Mock<Repositories.Repository.Interfaces.IReadRepository<CostEstimateGroup>> _groupRepoMock = new();
    private readonly Mock<Repositories.Repository.Interfaces.IReadRepository<CostEstimateItem>> _itemRepoMock = new();
    private readonly Mock<Repositories.Repository.Interfaces.IReadRepository<CostEstimateGroupFieldValue>> _groupFvRepoMock = new();
    private readonly Mock<Repositories.Repository.Interfaces.IReadRepository<CostEstimateItemFieldValue>> _itemFvRepoMock = new();
    private readonly CostEstimateCacheService _sut;

    public CostEstimateCacheServiceTests()
    {
        _sut = new CostEstimateCacheService(
            _cacheMock.Object,
            _ceRepoMock.Object,
            _templateRepoMock.Object,
            _groupRepoMock.Object,
            _itemRepoMock.Object,
            _groupFvRepoMock.Object,
            _itemFvRepoMock.Object,
            NullLogger<CostEstimateCacheService>.Instance);
    }

    // ─── GetCostEstimateAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetCostEstimateAsync_CallsCacheWithCorrectKey()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid ceId = Guid.NewGuid();
        string expectedKey = CostEstimateCacheKeys.CostEstimate(tenantId, projectId, ceId);
        CostEstimate ce = new() { Id = ceId };

        _cacheMock
            .Setup(c => c.GetOrAddAsync(
                expectedKey,
                It.IsAny<Func<Task<CostEstimate>>>(),
                CostEstimateCacheKeys.Ttl,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ce);

        // Act
        CostEstimate? result = await _sut.GetCostEstimateAsync(ceId, tenantId, projectId, CancellationToken.None);

        // Assert
        result.Should().Be(ce);
        _cacheMock.Verify(c => c.GetOrAddAsync(
            expectedKey,
            It.IsAny<Func<Task<CostEstimate>>>(),
            CostEstimateCacheKeys.Ttl,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCostEstimateAsync_WhenCacheReturnsNull_ReturnsNull()
    {
        // Arrange
        _cacheMock
            .Setup(c => c.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<CostEstimate>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CostEstimate?)null);

        // Act
        CostEstimate? result = await _sut.GetCostEstimateAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    // ─── GetTemplateAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetTemplateAsync_CallsCacheWithCorrectKey()
    {
        // Arrange
        Guid templateId = Guid.NewGuid();
        string expectedKey = CostEstimateCacheKeys.Template(templateId);
        CostEstimateTemplate template = new() { Id = templateId };

        _cacheMock
            .Setup(c => c.GetOrAddAsync(
                expectedKey,
                It.IsAny<Func<Task<CostEstimateTemplate>>>(),
                CostEstimateCacheKeys.Ttl,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        // Act
        CostEstimateTemplate? result = await _sut.GetTemplateAsync(templateId, CancellationToken.None);

        // Assert
        result.Should().Be(template);
    }

    // ─── GetGroupsDictionaryAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetGroupsDictionaryAsync_WhenCacheReturnsValue_ReturnsIt()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid ceId = Guid.NewGuid();
        string expectedKey = CostEstimateCacheKeys.Groups(tenantId, projectId, ceId);
        Guid groupId = Guid.NewGuid();
        Dictionary<Guid, CostEstimateGroup> dict = new()
        {
            [groupId] = new CostEstimateGroup { Id = groupId }
        };

        _cacheMock
            .Setup(c => c.GetOrAddAsync(
                expectedKey,
                It.IsAny<Func<Task<Dictionary<Guid, CostEstimateGroup>>>>(),
                CostEstimateCacheKeys.Ttl,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dict);

        // Act
        Dictionary<Guid, CostEstimateGroup> result = await _sut.GetGroupsDictionaryAsync(
            ceId, tenantId, projectId, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainKey(groupId);
    }

    [Fact]
    public async Task GetGroupsDictionaryAsync_WhenCacheReturnsNull_ReturnsEmptyDictionary()
    {
        // Arrange
        _cacheMock
            .Setup(c => c.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<Dictionary<Guid, CostEstimateGroup>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dictionary<Guid, CostEstimateGroup>?)null);

        // Act
        Dictionary<Guid, CostEstimateGroup> result = await _sut.GetGroupsDictionaryAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    // ─── InvalidateCostEstimateAsync ─────────────────────────────────────────

    [Fact]
    public async Task InvalidateCostEstimateAsync_RemovesAllFiveCacheKeys()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid ceId = Guid.NewGuid();

        _cacheMock
            .Setup(c => c.RemoveCacheByKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.InvalidateCostEstimateAsync(ceId, tenantId, projectId, CancellationToken.None);

        // Assert — all 5 cache keys removed
        _cacheMock.Verify(c => c.RemoveCacheByKeyAsync(
            CostEstimateCacheKeys.CostEstimate(tenantId, projectId, ceId),
            It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveCacheByKeyAsync(
            CostEstimateCacheKeys.Groups(tenantId, projectId, ceId),
            It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveCacheByKeyAsync(
            CostEstimateCacheKeys.Items(tenantId, projectId, ceId),
            It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveCacheByKeyAsync(
            CostEstimateCacheKeys.GroupFieldValues(tenantId, projectId, ceId),
            It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveCacheByKeyAsync(
            CostEstimateCacheKeys.ItemFieldValues(tenantId, projectId, ceId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Granular invalidation methods ───────────────────────────────────────

    [Fact]
    public async Task InvalidateGroupsAsync_RemovesGroupsKey()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid ceId = Guid.NewGuid();
        string expectedKey = CostEstimateCacheKeys.Groups(tenantId, projectId, ceId);

        _cacheMock
            .Setup(c => c.RemoveCacheByKeyAsync(expectedKey, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.InvalidateGroupsAsync(ceId, tenantId, projectId, CancellationToken.None);

        // Assert
        _cacheMock.Verify(c => c.RemoveCacheByKeyAsync(expectedKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateItemsAsync_RemovesItemsKey()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid ceId = Guid.NewGuid();
        string expectedKey = CostEstimateCacheKeys.Items(tenantId, projectId, ceId);

        _cacheMock
            .Setup(c => c.RemoveCacheByKeyAsync(expectedKey, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.InvalidateItemsAsync(ceId, tenantId, projectId, CancellationToken.None);

        // Assert
        _cacheMock.Verify(c => c.RemoveCacheByKeyAsync(expectedKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateTemplateAsync_RemovesTemplateKey()
    {
        // Arrange
        Guid templateId = Guid.NewGuid();
        string expectedKey = CostEstimateCacheKeys.Template(templateId);

        _cacheMock
            .Setup(c => c.RemoveCacheByKeyAsync(expectedKey, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.InvalidateTemplateAsync(templateId, CancellationToken.None);

        // Assert
        _cacheMock.Verify(c => c.RemoveCacheByKeyAsync(expectedKey, It.IsAny<CancellationToken>()), Times.Once);
    }
}
