using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using CQRS.WorkSchedules.DeleteWorkSchedule;
using Entities.Models.CostTrackers;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.WorkSchedules;

public sealed class DeleteWorkScheduleCommandHandlerTests
{
    private readonly Mock<IRepository<WorkSchedule>> _workScheduleRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStage>> _stageRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWork>> _stageWorkRepoMock = new();
    private readonly Mock<IRepository<TrackedCost>> _trackedCostRepoMock = new();
    private readonly Mock<IWorkScheduleCacheService> _scheduleCacheMock = new();
    private readonly Mock<IWorkScheduleAccessService> _accessServiceMock = new();
    private readonly DeleteWorkScheduleCommandHandler _handler;

    public DeleteWorkScheduleCommandHandlerTests()
    {
        _stageRepoMock
            .Setup(r => r.SelectAsync(
                It.IsAny<Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<Expression<Func<WorkScheduleStage, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        _stageWorkRepoMock
            .Setup(r => r.SelectAsync(
                It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Expression<Func<WorkScheduleStageWork, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        _handler = new DeleteWorkScheduleCommandHandler(
            _workScheduleRepoMock.Object,
            _stageRepoMock.Object,
            _stageWorkRepoMock.Object,
            _trackedCostRepoMock.Object,
            _scheduleCacheMock.Object,
            _accessServiceMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static DeleteWorkScheduleCommand ValidCommand() =>
        new DeleteWorkScheduleCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            WorkScheduleId = Guid.NewGuid()
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenWorkScheduleExists_SoftDeletesAndInvalidatesCache()
    {
        // Arrange
        DeleteWorkScheduleCommand command = ValidCommand();
        WorkSchedule workSchedule = new WorkSchedule
        {
            Id = command.WorkScheduleId,
            TenantId = command.TenantId,
            ProjectId = command.ProjectId,
            Name = "To Delete"
        };

        _workScheduleRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<WorkSchedule, bool>>>()))
            .ReturnsAsync(workSchedule);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        workSchedule.IsDeleted.Should().BeTrue();
        _workScheduleRepoMock.Verify(r => r.Update(workSchedule), Times.Once);
        _scheduleCacheMock.Verify(c => c.InvalidateScheduleAsync(command.WorkScheduleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenWorkScheduleNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        DeleteWorkScheduleCommand command = ValidCommand();

        _workScheduleRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<WorkSchedule, bool>>>()))
            .ReturnsAsync((WorkSchedule?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }
}
