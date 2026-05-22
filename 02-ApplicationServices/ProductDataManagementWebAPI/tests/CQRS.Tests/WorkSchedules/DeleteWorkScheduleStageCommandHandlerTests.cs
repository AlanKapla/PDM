using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using CQRS.WorkSchedules.DeleteWorkScheduleStage;
using Entities.Models.CostTrackers;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.WorkSchedules;

public sealed class DeleteWorkScheduleStageCommandHandlerTests
{
    private readonly Mock<IRepository<WorkScheduleStage>> _stageRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWork>> _workRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWorkDependency>> _dependencyRepoMock = new();
    private readonly Mock<IRepository<TrackedCost>> _trackedCostRepoMock = new();
    private readonly Mock<IWorkScheduleCacheService> _scheduleCacheMock = new();
    private readonly Mock<IWorkScheduleAccessService> _accessServiceMock = new();
    private readonly DeleteWorkScheduleStageCommandHandler _handler;

    public DeleteWorkScheduleStageCommandHandlerTests()
    {
        _workRepoMock
            .Setup(r => r.SelectAsync(
                It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Expression<Func<WorkScheduleStageWork, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        _handler = new DeleteWorkScheduleStageCommandHandler(
            _stageRepoMock.Object,
            _workRepoMock.Object,
            _dependencyRepoMock.Object,
            _trackedCostRepoMock.Object,
            _scheduleCacheMock.Object,
            _accessServiceMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static DeleteWorkScheduleStageCommand ValidCommand(Guid? stageId = null) =>
        new DeleteWorkScheduleStageCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            WorkScheduleId = Guid.NewGuid(),
            WorkScheduleStageId = stageId ?? Guid.NewGuid()
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenStageExists_DeletesStageAndInvalidatesCache()
    {
        // Arrange
        DeleteWorkScheduleStageCommand command = ValidCommand();
        WorkScheduleStage stage = new WorkScheduleStage
        {
            Id = command.WorkScheduleStageId,
            TenantId = command.TenantId,
            WorkScheduleId = command.WorkScheduleId,
            Name = "Stage 1"
        };

        _stageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<WorkScheduleStage, bool>>>()))
            .ReturnsAsync(new List<WorkScheduleStage> { stage });

        _stageRepoMock
            .Setup(r => r.ExecuteUpdateAsync(
                It.IsAny<Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<WorkScheduleStage>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _stageRepoMock
            .Setup(r => r.ExecuteDeleteAsync(
                It.IsAny<Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _scheduleCacheMock.Verify(c => c.InvalidateScheduleAsync(command.WorkScheduleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenStageNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        DeleteWorkScheduleStageCommand command = ValidCommand();

        _stageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<WorkScheduleStage, bool>>>()))
            .ReturnsAsync(new List<WorkScheduleStage>());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }
}
