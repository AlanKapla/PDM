using Business.Implementation.Services;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using FluentAssertions;
using Moq;

namespace Business.Tests.Services;

public class CostEstimateShareServiceTests
{
    private readonly Mock<ICurrentUser> _currentUserMock = new Mock<ICurrentUser>();
    private readonly Mock<ICostEstimateAccessService> _accessServiceMock = new Mock<ICostEstimateAccessService>();
    private readonly CostEstimateShareService _sut;

    public CostEstimateShareServiceTests()
    {
        _sut = new CostEstimateShareService(_currentUserMock.Object, _accessServiceMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static CostEstimate BuildCostEstimate(Guid ownerId, Guid? tenantId = null, Guid? projectId = null)
    {
        return new CostEstimate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId ?? Guid.NewGuid(),
            ProjectId = projectId ?? Guid.NewGuid(),
            OwnerId = ownerId,
            Name = "Test Estimate"
        };
    }

    // ─── ValidateOwnerOrAdminAsync ────────────────────────────────────────────

    [Fact]
    public async Task ValidateOwnerOrAdminAsync_CurrentUserIsOwner_DoesNotThrow()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        CostEstimate estimate = BuildCostEstimate(ownerId: userId);
        _currentUserMock.Setup(u => u.Id).Returns(userId);
        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(estimate.TenantId, estimate.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _sut.ValidateOwnerOrAdminAsync(estimate, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateOwnerOrAdminAsync_CurrentUserIsAdmin_NotOwner_DoesNotThrow()
    {
        // Arrange
        Guid ownerId = Guid.NewGuid();
        Guid adminUserId = Guid.NewGuid();
        CostEstimate estimate = BuildCostEstimate(ownerId: ownerId);
        _currentUserMock.Setup(u => u.Id).Returns(adminUserId);
        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(estimate.TenantId, estimate.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _sut.ValidateOwnerOrAdminAsync(estimate, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateOwnerOrAdminAsync_NotOwnerNotAdmin_ThrowsForbiddenApiException()
    {
        // Arrange
        Guid ownerId = Guid.NewGuid();
        Guid regularUserId = Guid.NewGuid();
        CostEstimate estimate = BuildCostEstimate(ownerId: ownerId);
        _currentUserMock.Setup(u => u.Id).Returns(regularUserId);
        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(estimate.TenantId, estimate.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _sut.ValidateOwnerOrAdminAsync(estimate, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>();
    }

    // ─── InvalidateAccessCacheAsync ───────────────────────────────────────────

    [Fact]
    public async Task InvalidateAccessCacheAsync_CallsBothCacheInvalidations()
    {
        // Arrange
        Guid costEstimateId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();

        _accessServiceMock
            .Setup(s => s.InvalidateCostEstimateAccessCacheAsync(tenantId, projectId, costEstimateId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _accessServiceMock
            .Setup(s => s.InvalidateAccessCacheAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.InvalidateAccessCacheAsync(costEstimateId, projectId, tenantId, CancellationToken.None);

        // Assert
        _accessServiceMock.Verify(
            s => s.InvalidateCostEstimateAccessCacheAsync(tenantId, projectId, costEstimateId, It.IsAny<CancellationToken>()),
            Times.Once);
        _accessServiceMock.Verify(
            s => s.InvalidateAccessCacheAsync(tenantId, projectId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
