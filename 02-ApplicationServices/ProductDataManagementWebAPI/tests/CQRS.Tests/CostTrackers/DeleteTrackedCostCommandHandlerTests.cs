using System.Linq.Expressions;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.CostTrackers.DeleteTrackedCost;
using Entities.Models.CostTrackers;
using Entities.Models.Costs;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.CostTrackers;

public sealed class DeleteTrackedCostCommandHandlerTests
{
    private readonly Mock<IRepository<TrackedCost>> _trackedCostRepoMock = new();
    private readonly Mock<IRepository<ProjectCost>> _projectCostRepoMock = new();
    private readonly Mock<IRepository<BaseCostAttachment>> _attachmentRepoMock = new();
    private readonly Mock<IBlobStorageService> _blobStorageServiceMock = new();
    private readonly Mock<IContractorService> _contractorServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<DeleteTrackedCostCommandHandler>> _loggerMock = new();
    private readonly DeleteTrackedCostCommandHandler _handler;

    public DeleteTrackedCostCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _handler = new DeleteTrackedCostCommandHandler(
            _trackedCostRepoMock.Object,
            _projectCostRepoMock.Object,
            _attachmentRepoMock.Object,
            _blobStorageServiceMock.Object,
            _contractorServiceMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static TrackedCost BuildCost(Guid tenantId, Guid projectId) =>
        new TrackedCost
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProjectId = projectId,
            Name = "Cost to Delete"
        };

    private static DeleteTrackedCostCommand BuildCommand(Guid tenantId, Guid projectId, Guid costId) =>
        new DeleteTrackedCostCommand
        {
            TenantId = tenantId,
            ProjectId = projectId,
            CostId = costId
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenTrackedCostExistsWithNoAttachments_SoftDeletesCostAndReturnsUnit()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        TrackedCost cost = BuildCost(tenantId, projectId);

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _trackedCostRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<TrackedCost, bool>>>()))
            .ReturnsAsync(cost);

        _attachmentRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<BaseCostAttachment, bool>>>()))
            .ReturnsAsync(new List<BaseCostAttachment>());

        DeleteTrackedCostCommand command = BuildCommand(tenantId, projectId, cost.Id);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        cost.IsDeleted.Should().BeTrue();
        cost.DeletedAt.Should().NotBeNull();
        _trackedCostRepoMock.Verify(r => r.Update(It.IsAny<TrackedCost>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTrackedCostNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _trackedCostRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<TrackedCost, bool>>>()))
            .ReturnsAsync((TrackedCost?)null);

        DeleteTrackedCostCommand command = BuildCommand(tenantId, projectId, Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenUserHasNoAccess_ThrowsForbiddenApiException()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        DeleteTrackedCostCommand command = BuildCommand(tenantId, projectId, Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>();
    }
}
