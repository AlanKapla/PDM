using System.Linq.Expressions;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostTrackers;
using CQRS.CostTrackers.UpdateTrackedCost;
using Entities.Models.CostEstimates;
using Entities.Models.CostTrackers;
using Entities.Models.Costs;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.CostTrackers;

public sealed class UpdateTrackedCostCommandHandlerTests
{
    private readonly Mock<IReadRepository<TrackedCost>> _trackedCostRepoMock = new();
    private readonly Mock<IReadRepository<CostEstimateItem>> _costEstimateItemRepoMock = new();
    private readonly Mock<IReadRepository<WorkScheduleStageWork>> _stageWorkRepoMock = new();
    private readonly Mock<ICostTrackerFinancialService> _financialServiceMock = new();
    private readonly Mock<ICostTrackerAttachmentService> _attachmentServiceMock = new();
    private readonly Mock<IContractorService> _contractorServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<UpdateTrackedCostCommandHandler>> _loggerMock = new();
    private readonly UpdateTrackedCostCommandHandler _handler;

    public UpdateTrackedCostCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _financialServiceMock
            .Setup(f => f.Calculate(It.IsAny<decimal?>(), It.IsAny<decimal?>()))
            .Returns<decimal?, decimal?>((n, g) => (n, g));

        _attachmentServiceMock
            .Setup(s => s.SyncAttachmentsAsync(
                It.IsAny<BaseCost>(),
                It.IsAny<IReadOnlyList<IFormFile>?>(),
                It.IsAny<IReadOnlyList<Guid>?>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BaseCostAttachment>());

        _handler = new UpdateTrackedCostCommandHandler(
            _trackedCostRepoMock.Object,
            _costEstimateItemRepoMock.Object,
            _stageWorkRepoMock.Object,
            _financialServiceMock.Object,
            _attachmentServiceMock.Object,
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
            Name = "Existing Cost"
        };

    private static UpdateTrackedCostCommand ValidCommand(Guid tenantId, Guid projectId, Guid costId) =>
        new UpdateTrackedCostCommand
        {
            TenantId = tenantId,
            ProjectId = projectId,
            CostId = costId,
            Name = "Updated Cost",
            Net = 200m,
            Gross = 246m,
            ClearAllAttachments = false
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenTrackedCostExists_UpdatesAndReturnsWeb()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        TrackedCost cost = BuildCost(tenantId, projectId);

        _currentUserMock
            .Setup(u => u.IsTenantOrProjectAdminAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _trackedCostRepoMock
            .As<IRepository<TrackedCost>>()
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<TrackedCost, bool>>>()))
            .ReturnsAsync(cost);

        UpdateTrackedCostCommand command = ValidCommand(tenantId, projectId, cost.Id);

        // Act
        TrackedCostWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(command.Name);
        result.Net.Should().Be(command.Net);
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
            .As<IRepository<TrackedCost>>()
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<TrackedCost, bool>>>()))
            .ReturnsAsync((TrackedCost?)null);

        UpdateTrackedCostCommand command = ValidCommand(tenantId, projectId, Guid.NewGuid());

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

        UpdateTrackedCostCommand command = ValidCommand(tenantId, projectId, Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>();
    }
}
