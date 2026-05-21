using Business.Implementation.Services;
using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;

namespace Business.Tests.Services;

public class CostEstimateAccessServiceTests
{
    private readonly Mock<IReadRepository<CostEstimate>> _ceRepoMock = new();
    private readonly Mock<IReadRepository<SharedCostEstimate>> _sharedCeRepoMock = new();
    private readonly Mock<ILogger<CostEstimateAccessService>> _loggerMock = new();
    private readonly ICacheService _passThruCache = new InvokeFactoryCacheService();
    private readonly CostEstimateAccessService _sut;

    public CostEstimateAccessServiceTests()
    {
        _sut = new CostEstimateAccessService(
            _passThruCache,
            _ceRepoMock.Object,
            _sharedCeRepoMock.Object,
            _loggerMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static Mock<ICurrentUser> BuildUser(Guid userId, bool isSuperAdmin = false)
    {
        Mock<ICurrentUser> mock = new();
        mock.Setup(u => u.Id).Returns(userId);
        mock.Setup(u => u.IsSuperAdmin).Returns(isSuperAdmin);
        return mock;
    }

    private static CostEstimate BuildCostEstimate(Guid tenantId, Guid projectId, Guid ownerId)
        => new CostEstimate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProjectId = projectId,
            OwnerId = ownerId,
            Name = "Test",
            IsDeleted = false
        };

    // ─── GetAccessLevelAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAccessLevelAsync_UserIsAdmin_ReturnsFull()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid ceId = Guid.NewGuid();
        Mock<ICurrentUser> user = BuildUser(userId);
        user.Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        CostEstimateAccessLevel result = await _sut.GetAccessLevelAsync(
            user.Object, tenantId, projectId, ceId, CancellationToken.None);

        // Assert
        result.Should().Be(CostEstimateAccessLevel.Full);
    }

    [Fact]
    public async Task GetAccessLevelAsync_UserIsOwner_ReturnsFull()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        CostEstimate ce = BuildCostEstimate(tenantId, projectId, ownerId: userId);

        Mock<ICurrentUser> user = BuildUser(userId);
        user.Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _ceRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimate, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ce);

        // Act
        CostEstimateAccessLevel result = await _sut.GetAccessLevelAsync(
            user.Object, tenantId, projectId, ce.Id, CancellationToken.None);

        // Assert
        result.Should().Be(CostEstimateAccessLevel.Full);
    }

    [Fact]
    public async Task GetAccessLevelAsync_CostEstimateNotFound_ReturnsNone()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid ceId = Guid.NewGuid();

        Mock<ICurrentUser> user = BuildUser(userId);
        user.Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _ceRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimate, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CostEstimate?)null);

        // Act
        CostEstimateAccessLevel result = await _sut.GetAccessLevelAsync(
            user.Object, tenantId, projectId, ceId, CancellationToken.None);

        // Assert
        result.Should().Be(CostEstimateAccessLevel.None);
    }

    [Fact]
    public async Task GetAccessLevelAsync_SharedWithUser_ReturnsRestricted()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid ownerId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        CostEstimate ce = BuildCostEstimate(tenantId, projectId, ownerId);

        Mock<ICurrentUser> user = BuildUser(userId);
        user.Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _ceRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimate, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ce);

        _sharedCeRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SharedCostEstimate, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        CostEstimateAccessLevel result = await _sut.GetAccessLevelAsync(
            user.Object, tenantId, projectId, ce.Id, CancellationToken.None);

        // Assert
        result.Should().Be(CostEstimateAccessLevel.Restricted);
    }

    [Fact]
    public async Task GetAccessLevelAsync_SuperAdmin_NotOwnerNotShared_ReturnsReadOnly()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid ownerId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        CostEstimate ce = BuildCostEstimate(tenantId, projectId, ownerId);

        Mock<ICurrentUser> user = BuildUser(userId, isSuperAdmin: true);
        user.Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _ceRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimate, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ce);

        _sharedCeRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SharedCostEstimate, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        CostEstimateAccessLevel result = await _sut.GetAccessLevelAsync(
            user.Object, tenantId, projectId, ce.Id, CancellationToken.None);

        // Assert
        result.Should().Be(CostEstimateAccessLevel.ReadOnly);
    }

    [Fact]
    public async Task GetAccessLevelAsync_RegularUser_NotOwnerNotShared_ReturnsNone()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid ownerId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        CostEstimate ce = BuildCostEstimate(tenantId, projectId, ownerId);

        Mock<ICurrentUser> user = BuildUser(userId, isSuperAdmin: false);
        user.Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _ceRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimate, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ce);

        _sharedCeRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SharedCostEstimate, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        CostEstimateAccessLevel result = await _sut.GetAccessLevelAsync(
            user.Object, tenantId, projectId, ce.Id, CancellationToken.None);

        // Assert
        result.Should().Be(CostEstimateAccessLevel.None);
    }

    // ─── GetAccessibleCostEstimateIdsAsync ────────────────────────────────────

    [Fact]
    public async Task GetAccessibleCostEstimateIdsAsync_ScopeAll_ReturnsAllProjectIds()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        List<Guid> expectedIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        Mock<ICurrentUser> user = BuildUser(userId);

        _ceRepoMock
            .Setup(r => r.GetIdsBySearchAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimate, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedIds);

        // Act
        HashSet<Guid> result = await _sut.GetAccessibleCostEstimateIdsAsync(
            user.Object, tenantId, projectId, ResourceScope.All, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedIds);
    }

    [Fact]
    public async Task GetAccessibleCostEstimateIdsAsync_ScopeMine_ReturnsOwnedIds()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        List<Guid> myIds = new List<Guid> { Guid.NewGuid() };

        Mock<ICurrentUser> user = BuildUser(userId);

        _ceRepoMock
            .Setup(r => r.GetIdsBySearchAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<CostEstimate, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(myIds);

        // Act
        HashSet<Guid> result = await _sut.GetAccessibleCostEstimateIdsAsync(
            user.Object, tenantId, projectId, ResourceScope.Mine, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(myIds);
    }

    [Fact]
    public async Task GetAccessibleCostEstimateIdsAsync_ScopeShared_ReturnsSharedIds()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        HashSet<Guid> sharedIds = new HashSet<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        Mock<ICurrentUser> user = BuildUser(userId);

        _sharedCeRepoMock
            .Setup(r => r.SelectToHashSetAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SharedCostEstimate, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<SharedCostEstimate, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(sharedIds);

        // Act
        HashSet<Guid> result = await _sut.GetAccessibleCostEstimateIdsAsync(
            user.Object, tenantId, projectId, ResourceScope.Shared, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(sharedIds);
    }

    [Fact]
    public async Task GetAccessibleCostEstimateIdsAsync_UnknownScope_ReturnsEmpty()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Mock<ICurrentUser> user = BuildUser(userId);

        // Act
        HashSet<Guid> result = await _sut.GetAccessibleCostEstimateIdsAsync(
            user.Object, Guid.NewGuid(), Guid.NewGuid(), (ResourceScope)99, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    // ─── FakeCacheService — executes factory directly (no caching) ────────────

    private sealed class InvokeFactoryCacheService : ICacheService
    {
        public async Task<T?> GetOrAddAsync<T>(
            string key,
            Func<Task<T>> factory,
            TimeSpan? expiration = null,
            CancellationToken cancellationToken = default) where T : class
            => await factory();

        public Task<Dictionary<string, T>> GetManyAsync<T>(
            IEnumerable<string> keys,
            CancellationToken cancellationToken = default) where T : class
            => Task.FromResult(new Dictionary<string, T>());

        public Task SetManyAsync<T>(
            Dictionary<string, T> items,
            TimeSpan? expiration = null,
            CancellationToken cancellationToken = default) where T : class
            => Task.CompletedTask;

        public Task RemoveCacheByKeyAsync(string key, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveCacheContainsAsync(string pattern, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
