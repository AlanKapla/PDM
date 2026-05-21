using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using CQRS.WorkSchedules.SetWorkScheduleStageWorkIsClosed;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.WorkSchedules;

public sealed class SetWorkScheduleStageWorkIsClosedCommandHandlerTests
{
    private readonly Mock<IRepository<WorkScheduleStageWork>> _workRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWorkPeriod>> _periodRepoMock = new();
    private readonly Mock<IWorkScheduleCacheService> _scheduleCacheMock = new();
    private readonly Mock<IWorkScheduleAccessService> _accessServiceMock = new();
    private readonly SetWorkScheduleStageWorkIsClosedCommandHandler _handler;

    public SetWorkScheduleStageWorkIsClosedCommandHandlerTests()
    {
        _handler = new SetWorkScheduleStageWorkIsClosedCommandHandler(
            _workRepoMock.Object,
            _periodRepoMock.Object,
            _scheduleCacheMock.Object,
            _accessServiceMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static SetWorkScheduleStageWorkIsClosedCommand ValidCommand() =>
        new SetWorkScheduleStageWorkIsClosedCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            WorkScheduleId = Guid.NewGuid(),
            WorkScheduleStageWorkId = Guid.NewGuid(),
            IsClosed = true
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenWorkExistsWithPeriods_UpdatesAllPeriodsAndInvalidatesCache()
    {
        // Arrange
        SetWorkScheduleStageWorkIsClosedCommand command = ValidCommand();
        WorkScheduleStageWork work = new WorkScheduleStageWork { Id = command.WorkScheduleStageWorkId };
        List<WorkScheduleStageWorkPeriod> periods = new List<WorkScheduleStageWorkPeriod>
        {
            new WorkScheduleStageWorkPeriod { Id = Guid.NewGuid(), IsClosed = false },
            new WorkScheduleStageWorkPeriod { Id = Guid.NewGuid(), IsClosed = false }
        };

        _workRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWork>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWork, object>>[]>()))
            .ReturnsAsync(work);

        _periodRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<WorkScheduleStageWorkPeriod, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWorkPeriod>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWorkPeriod, object>>[]>()))
            .ReturnsAsync(periods);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _periodRepoMock.Verify(r => r.UpdateRange(It.IsAny<IEnumerable<WorkScheduleStageWorkPeriod>>()), Times.Once);
        _periodRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _scheduleCacheMock.Verify(c => c.InvalidateScheduleAsync(command.WorkScheduleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenWorkNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        SetWorkScheduleStageWorkIsClosedCommand command = ValidCommand();

        _workRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWork>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWork, object>>[]>()))
            .ReturnsAsync((WorkScheduleStageWork?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenNoPeriods_DoesNotCallUpdateRange()
    {
        // Arrange
        SetWorkScheduleStageWorkIsClosedCommand command = ValidCommand();
        WorkScheduleStageWork work = new WorkScheduleStageWork { Id = command.WorkScheduleStageWorkId };

        _workRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWork>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWork, object>>[]>()))
            .ReturnsAsync(work);

        _periodRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<WorkScheduleStageWorkPeriod, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWorkPeriod>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWorkPeriod, object>>[]>()))
            .ReturnsAsync(new List<WorkScheduleStageWorkPeriod>());

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _periodRepoMock.Verify(r => r.UpdateRange(It.IsAny<IEnumerable<WorkScheduleStageWorkPeriod>>()), Times.Never);
        _periodRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _scheduleCacheMock.Verify(c => c.InvalidateScheduleAsync(command.WorkScheduleId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
