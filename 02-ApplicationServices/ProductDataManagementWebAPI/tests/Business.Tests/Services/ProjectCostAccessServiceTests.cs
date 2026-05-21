using Business.Implementation.Services;
using Business.Interfaces.Model;
using Entities.Models.Costs;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;

namespace Business.Tests.Services;

public class ProjectCostAccessServiceTests
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IReadRepository<SharedProjectCost>> _sharedRepoMock = new();
    private readonly ProjectCostAccessService _sut;

    public ProjectCostAccessServiceTests()
    {
        _sut = new ProjectCostAccessService(
            _currentUserMock.Object,
            _sharedRepoMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ProjectCost BuildCost(Guid tenantId, Guid projectId, Guid userId)
        => new ProjectCost
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProjectId = projectId,
            UserId = userId,
            Name = "Test cost"
        };

    // ─── HasWriteAccessAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task HasWriteAccessAsync_UserIsOwner_ReturnsTrue()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        ProjectCost cost = BuildCost(Guid.NewGuid(), Guid.NewGuid(), userId);

        // Act
        bool result = await _sut.HasWriteAccessAsync(cost, userId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasWriteAccessAsync_UserIsAdmin_ReturnsTrue()
    {
        // Arrange
        Guid ownerId = Guid.NewGuid();
        Guid adminId = Guid.NewGuid();
        ProjectCost cost = BuildCost(Guid.NewGuid(), Guid.NewGuid(), ownerId);

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(cost.TenantId, cost.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        bool result = await _sut.HasWriteAccessAsync(cost, adminId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasWriteAccessAsync_NotOwnerNotAdmin_ReturnsFalse()
    {
        // Arrange
        Guid ownerId = Guid.NewGuid();
        Guid regularUserId = Guid.NewGuid();
        ProjectCost cost = BuildCost(Guid.NewGuid(), Guid.NewGuid(), ownerId);

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(cost.TenantId, cost.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        bool result = await _sut.HasWriteAccessAsync(cost, regularUserId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    // ─── HasShareAccessAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task HasShareAccessAsync_UserIsOwner_ReturnsTrue()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        ProjectCost cost = BuildCost(Guid.NewGuid(), Guid.NewGuid(), userId);

        // Act
        bool result = await _sut.HasShareAccessAsync(cost, userId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasShareAccessAsync_UserIsAdmin_ReturnsTrue()
    {
        // Arrange
        Guid ownerId = Guid.NewGuid();
        Guid adminId = Guid.NewGuid();
        ProjectCost cost = BuildCost(Guid.NewGuid(), Guid.NewGuid(), ownerId);

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(cost.TenantId, cost.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        bool result = await _sut.HasShareAccessAsync(cost, adminId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasShareAccessAsync_CostSharedWithUser_ReturnsTrue()
    {
        // Arrange
        Guid ownerId = Guid.NewGuid();
        Guid sharedUserId = Guid.NewGuid();
        ProjectCost cost = BuildCost(Guid.NewGuid(), Guid.NewGuid(), ownerId);

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(cost.TenantId, cost.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        SharedProjectCost share = new SharedProjectCost
        {
            Id = Guid.NewGuid(),
            ProjectCostId = cost.Id,
            SharedWithUserId = sharedUserId
        };

        _sharedRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<SharedProjectCost, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(share);

        // Act
        bool result = await _sut.HasShareAccessAsync(cost, sharedUserId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasShareAccessAsync_CostNotSharedWithUser_ReturnsFalse()
    {
        // Arrange
        Guid ownerId = Guid.NewGuid();
        Guid otherUserId = Guid.NewGuid();
        ProjectCost cost = BuildCost(Guid.NewGuid(), Guid.NewGuid(), ownerId);

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(cost.TenantId, cost.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _sharedRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<SharedProjectCost, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((SharedProjectCost?)null);

        // Act
        bool result = await _sut.HasShareAccessAsync(cost, otherUserId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }
}
