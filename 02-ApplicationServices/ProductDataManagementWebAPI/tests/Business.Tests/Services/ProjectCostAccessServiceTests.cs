using Business.Implementation.Services;
using Business.Interfaces.Model;
using Entities.Models.Costs;
using FluentAssertions;
using Moq;

namespace Business.Tests.Services;

public class ProjectCostAccessServiceTests
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly ProjectCostAccessService _sut;

    public ProjectCostAccessServiceTests()
    {
        _sut = new ProjectCostAccessService(
            _currentUserMock.Object);
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
}
