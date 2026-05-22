using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using CQRS.WorkSchedules.MoveWorkScheduleStageWork;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.WorkSchedules;

public sealed class MoveWorkScheduleStageWorkCommandHandlerTests
{
    private readonly Mock<IRepository<WorkScheduleStage>> _stageRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWork>> _workRepoMock = new();
    private readonly Mock<IWorkScheduleCacheService> _scheduleCacheMock = new();
    private readonly Mock<IWorkScheduleAccessService> _accessServiceMock = new();
    private readonly MoveWorkScheduleStageWorkCommandHandler _handler;

    public MoveWorkScheduleStageWorkCommandHandlerTests()
    {
        _handler = new MoveWorkScheduleStageWorkCommandHandler(
            _stageRepoMock.Object,
            _workRepoMock.Object,
            _scheduleCacheMock.Object,
            _accessServiceMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static MoveWorkScheduleStageWorkCommand ValidCommand(Guid workId, Guid targetStageId) =>
        new MoveWorkScheduleStageWorkCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            WorkScheduleId = Guid.NewGuid(),
            WorkScheduleStageWorkId = workId,
            TargetStageId = targetStageId,
            TargetOrder = 0
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenValidMove_UpdatesWorkAndInvalidatesCache()
    {
        // Arrange
        Guid workId = Guid.NewGuid();
        Guid sourceStageId = Guid.NewGuid();
        Guid targetStageId = Guid.NewGuid();
        MoveWorkScheduleStageWorkCommand command = ValidCommand(workId, targetStageId);

        WorkScheduleStageWork work = new WorkScheduleStageWork
        {
            Id = workId,
            WorkScheduleStageId = sourceStageId,
            Order = 2
        };

        _stageRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _workRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWork>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWork, object>>[]>()))
            .ReturnsAsync(work);

        _workRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWork>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWork, object>>[]>()))
            .ReturnsAsync(new List<WorkScheduleStageWork>());

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        work.WorkScheduleStageId.Should().Be(targetStageId);
        _workRepoMock.Verify(r => r.Update(work), Times.Once);
        _workRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _scheduleCacheMock.Verify(c => c.InvalidateScheduleAsync(command.WorkScheduleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTargetStageNotFound_ThrowsValidationApiException()
    {
        // Arrange
        Guid workId = Guid.NewGuid();
        Guid targetStageId = Guid.NewGuid();
        MoveWorkScheduleStageWorkCommand command = ValidCommand(workId, targetStageId);

        _stageRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>();
    }

    [Fact]
    public async Task Handle_WhenWorkNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        Guid workId = Guid.NewGuid();
        Guid targetStageId = Guid.NewGuid();
        MoveWorkScheduleStageWorkCommand command = ValidCommand(workId, targetStageId);

        _stageRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

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
}
