using System.Linq.Expressions;
using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.CostEstimates.DeleteCostEstimate;
using Entities.Models.CostEstimates;
using Entities.Models.CostTrackers;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.CostEstimates;

public sealed class DeleteCostEstimateCommandHandlerTests
{
    private readonly Mock<IRepository<CostEstimate>> _costEstimateRepoMock = new();
    private readonly Mock<IRepository<CostEstimateGroup>> _groupRepoMock = new();
    private readonly Mock<IRepository<CostEstimateItem>> _itemRepoMock = new();
    private readonly Mock<IRepository<SharedCostEstimate>> _sharedCeRepoMock = new();
    private readonly Mock<IRepository<WorkSchedule>> _workScheduleRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStage>> _stageRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWork>> _stageWorkRepoMock = new();
    private readonly Mock<IRepository<TrackedCost>> _trackedCostRepoMock = new();
    private readonly Mock<ICostEstimateAccessService> _ceAccessServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly DeleteCostEstimateCommandHandler _handler;

    public DeleteCostEstimateCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _groupRepoMock
            .Setup(r => r.SelectAsync(
                It.IsAny<Expression<Func<CostEstimateGroup, bool>>>(),
                It.IsAny<Expression<Func<CostEstimateGroup, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        _itemRepoMock
            .Setup(r => r.SelectAsync(
                It.IsAny<Expression<Func<CostEstimateItem, bool>>>(),
                It.IsAny<Expression<Func<CostEstimateItem, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        _handler = new DeleteCostEstimateCommandHandler(
            _costEstimateRepoMock.Object,
            _groupRepoMock.Object,
            _itemRepoMock.Object,
            _sharedCeRepoMock.Object,
            _workScheduleRepoMock.Object,
            _stageRepoMock.Object,
            _stageWorkRepoMock.Object,
            _trackedCostRepoMock.Object,
            _ceAccessServiceMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static CostEstimate BuildCostEstimate() =>
        new CostEstimate
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Name = "Test CE",
            Status = CostEstimateStatus.Draft,
            IsDeleted = false
        };

    private static DeleteCostEstimateCommand ValidCommand(CostEstimate costEstimate) =>
        new DeleteCostEstimateCommand
        {
            TenantId = costEstimate.TenantId,
            ProjectId = costEstimate.ProjectId,
            CostEstimateId = costEstimate.Id
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenFullAccess_SoftDeletesCostEstimate()
    {
        // Arrange
        CostEstimate costEstimate = BuildCostEstimate();
        DeleteCostEstimateCommand command = ValidCommand(costEstimate);

        _costEstimateRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<CostEstimate, bool>>>()))
            .ReturnsAsync(costEstimate);

        _ceAccessServiceMock
            .Setup(s => s.GetAccessLevelAsync(
                It.IsAny<ICurrentUser>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostEstimateAccessLevel.Full);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        costEstimate.IsDeleted.Should().BeTrue();
        _costEstimateRepoMock.Verify(r => r.Update(costEstimate), Times.Once);
        _sharedCeRepoMock.Verify(r => r.ExecuteDeleteAsync(
            It.IsAny<Expression<Func<SharedCostEstimate, bool>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCostEstimateNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _costEstimateRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<CostEstimate, bool>>>()))
            .ReturnsAsync((CostEstimate?)null);

        DeleteCostEstimateCommand command = new DeleteCostEstimateCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            CostEstimateId = Guid.NewGuid()
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenAccessLevelIsNotFull_ThrowsForbiddenApiException()
    {
        // Arrange
        CostEstimate costEstimate = BuildCostEstimate();
        DeleteCostEstimateCommand command = ValidCommand(costEstimate);

        _costEstimateRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<CostEstimate, bool>>>()))
            .ReturnsAsync(costEstimate);

        _ceAccessServiceMock
            .Setup(s => s.GetAccessLevelAsync(
                It.IsAny<ICurrentUser>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostEstimateAccessLevel.Restricted);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>();
    }
}
