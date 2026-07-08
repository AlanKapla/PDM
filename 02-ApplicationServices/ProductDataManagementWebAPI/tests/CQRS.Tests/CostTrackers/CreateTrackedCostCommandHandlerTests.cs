using System.Linq.Expressions;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostTrackers;
using CQRS.CostTrackers.CreateTrackedCost;
using Entities.Models.CostEstimates;
using Entities.Models.CostTrackers;
using Entities.Models.Costs;
using Entities.Models.Projects;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.CostTrackers;

public sealed class CreateTrackedCostCommandHandlerTests
{
    private readonly Mock<IReadRepository<TrackedCost>> _trackedCostRepoMock = new();
    private readonly Mock<IReadRepository<ProjectCostCategory>> _categoryRepoMock = new();
    private readonly Mock<IReadRepository<CostEstimateItem>> _costEstimateItemRepoMock = new();
    private readonly Mock<IReadRepository<WorkScheduleStageWork>> _stageWorkRepoMock = new();
    private readonly Mock<ICostTrackerFinancialService> _financialServiceMock = new();
    private readonly Mock<ICostTrackerAttachmentService> _attachmentServiceMock = new();
    private readonly Mock<IContractorService> _contractorServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<CreateTrackedCostCommandHandler>> _loggerMock = new();
    private readonly CreateTrackedCostCommandHandler _handler;

    public CreateTrackedCostCommandHandlerTests()
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

        _handler = new CreateTrackedCostCommandHandler(
            _trackedCostRepoMock.Object,
            _categoryRepoMock.Object,
            _costEstimateItemRepoMock.Object,
            _stageWorkRepoMock.Object,
            _financialServiceMock.Object,
            _attachmentServiceMock.Object,
            _contractorServiceMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static CreateTrackedCostCommand ValidCommand(
        Guid? costEstimateItemId = null,
        Guid? workScheduleStageWorkId = null) =>
        new CreateTrackedCostCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Name = "Test Cost",
            Number = "TC-001",
            Net = 100m,
            Gross = 123m,
            CostEstimateItemId = costEstimateItemId,
            WorkScheduleStageWorkId = workScheduleStageWorkId
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenCommandHasNoLinks_CreatesTrackedCostAndReturnsWeb()
    {
        // Arrange
        CreateTrackedCostCommand command = ValidCommand();

        // Act
        TrackedCostWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(command.Name);
        result.Net.Should().Be(command.Net);
        result.Gross.Should().Be(command.Gross);
        _trackedCostRepoMock.Verify(r => r.Insert(It.IsAny<TrackedCost>()), Times.Once);
        _trackedCostRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCostEstimateItemNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        CreateTrackedCostCommand command = ValidCommand(costEstimateItemId: Guid.NewGuid());

        _costEstimateItemRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<CostEstimateItem, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenWorkScheduleStageWorkNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        CreateTrackedCostCommand command = ValidCommand(workScheduleStageWorkId: Guid.NewGuid());

        _stageWorkRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }
}
