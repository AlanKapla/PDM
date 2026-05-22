using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using CQRS.WorkSchedules.SetWorkScheduleStageWorkPeriods;
using CQRS.WorkSchedules.Shared;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.WorkSchedules;

public sealed class SetWorkScheduleStageWorkPeriodsCommandHandlerTests
{
    private readonly Mock<IRepository<WorkScheduleStageWork>> _workRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWorkPeriod>> _periodRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWorkDependency>> _dependencyRepoMock = new();
    private readonly Mock<IWorkScheduleCacheService> _scheduleCacheMock = new();
    private readonly Mock<IWorkScheduleAccessService> _accessServiceMock = new();
    private readonly SetWorkScheduleStageWorkPeriodsCommandHandler _handler;

    public SetWorkScheduleStageWorkPeriodsCommandHandlerTests()
    {
        _handler = new SetWorkScheduleStageWorkPeriodsCommandHandler(
            _workRepoMock.Object,
            _periodRepoMock.Object,
            _dependencyRepoMock.Object,
            _scheduleCacheMock.Object,
            _accessServiceMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static SetWorkScheduleStageWorkPeriodsCommand ValidCommand(List<WorkPeriodDto>? periods = null) =>
        new SetWorkScheduleStageWorkPeriodsCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            WorkScheduleId = Guid.NewGuid(),
            WorkScheduleStageWorkId = Guid.NewGuid(),
            Periods = periods ?? new List<WorkPeriodDto>()
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenWorkNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodsCommand command = ValidCommand();

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
    public async Task Handle_WhenPeriodsProvided_DeletesOldAndInsertsNewAndUpdatesWork()
    {
        // Arrange
        DateTime start = new DateTime(2025, 1, 1);
        DateTime end = new DateTime(2025, 1, 31);
        List<WorkPeriodDto> newPeriods = new List<WorkPeriodDto>
        {
            new WorkPeriodDto(start, end, false)
        };
        SetWorkScheduleStageWorkPeriodsCommand command = ValidCommand(newPeriods);
        WorkScheduleStageWork work = new WorkScheduleStageWork { Id = command.WorkScheduleStageWorkId };

        _workRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWork>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWork, object>>[]>()))
            .ReturnsAsync(work);

        _dependencyRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<WorkScheduleStageWorkDependency, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWorkDependency>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWorkDependency, object>>[]>()))
            .ReturnsAsync(new List<WorkScheduleStageWorkDependency>());

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        work.PlannedStartDate.Should().Be(start);
        work.PlannedEndDate.Should().Be(end);
        _periodRepoMock.Verify(r => r.ExecuteDeleteAsync(
            It.IsAny<Expression<Func<WorkScheduleStageWorkPeriod, bool>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _periodRepoMock.Verify(r => r.InsertRange(It.IsAny<IEnumerable<WorkScheduleStageWorkPeriod>>()), Times.Once);
        _workRepoMock.Verify(r => r.Update(work), Times.Once);
        _workRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _scheduleCacheMock.Verify(c => c.InvalidateScheduleAsync(command.WorkScheduleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmptyPeriods_SetsNullDatesAndDoesNotInsert()
    {
        // Arrange
        SetWorkScheduleStageWorkPeriodsCommand command = ValidCommand(new List<WorkPeriodDto>());
        WorkScheduleStageWork work = new WorkScheduleStageWork
        {
            Id = command.WorkScheduleStageWorkId,
            PlannedStartDate = DateTime.UtcNow,
            PlannedEndDate = DateTime.UtcNow.AddDays(10)
        };

        _workRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWork>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWork, object>>[]>()))
            .ReturnsAsync(work);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        work.PlannedStartDate.Should().BeNull();
        work.PlannedEndDate.Should().BeNull();
        _periodRepoMock.Verify(r => r.InsertRange(It.IsAny<IEnumerable<WorkScheduleStageWorkPeriod>>()), Times.Never);
    }
}
