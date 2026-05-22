using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using CQRS.WorkSchedules.MoveWorkScheduleStage;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.WorkSchedules;

public sealed class MoveWorkScheduleStageCommandHandlerTests
{
    private readonly Mock<IRepository<WorkScheduleStage>> _stageRepoMock = new();
    private readonly Mock<IWorkScheduleCacheService> _scheduleCacheMock = new();
    private readonly Mock<IWorkScheduleAccessService> _accessServiceMock = new();
    private readonly MoveWorkScheduleStageCommandHandler _handler;

    public MoveWorkScheduleStageCommandHandlerTests()
    {
        _handler = new MoveWorkScheduleStageCommandHandler(
            _stageRepoMock.Object,
            _scheduleCacheMock.Object,
            _accessServiceMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static MoveWorkScheduleStageCommand ValidCommand(Guid stageId, Guid? parentStageId = null) =>
        new MoveWorkScheduleStageCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            WorkScheduleId = Guid.NewGuid(),
            WorkScheduleStageId = stageId,
            ParentStageId = parentStageId
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenMovingToRoot_ClearsParentAndSaves()
    {
        // Arrange
        Guid stageId = Guid.NewGuid();
        MoveWorkScheduleStageCommand command = ValidCommand(stageId, parentStageId: null);

        WorkScheduleStage stage = new WorkScheduleStage { Id = stageId, ParentStageId = Guid.NewGuid() };

        _stageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStage>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStage, object>>[]>()))
            .ReturnsAsync(new List<WorkScheduleStage> { stage });

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        stage.ParentStageId.Should().BeNull();
        _stageRepoMock.Verify(r => r.Update(stage), Times.Once);
        _stageRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _scheduleCacheMock.Verify(c => c.InvalidateScheduleAsync(command.WorkScheduleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenStageNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        Guid stageId = Guid.NewGuid();
        MoveWorkScheduleStageCommand command = ValidCommand(stageId);

        _stageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStage>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStage, object>>[]>()))
            .ReturnsAsync(new List<WorkScheduleStage>());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenParentStageNotInSchedule_ThrowsValidationApiException()
    {
        // Arrange
        Guid stageId = Guid.NewGuid();
        Guid parentId = Guid.NewGuid();
        MoveWorkScheduleStageCommand command = ValidCommand(stageId, parentStageId: parentId);

        WorkScheduleStage stage = new WorkScheduleStage { Id = stageId };
        // Parent is NOT in the returned list

        _stageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStage>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStage, object>>[]>()))
            .ReturnsAsync(new List<WorkScheduleStage> { stage });

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>();
    }

    [Fact]
    public async Task Handle_WhenMovingUnderDescendant_ThrowsValidationApiException()
    {
        // Arrange
        Guid stageId = Guid.NewGuid();
        Guid childId = Guid.NewGuid();
        MoveWorkScheduleStageCommand command = ValidCommand(stageId, parentStageId: childId);

        WorkScheduleStage stage = new WorkScheduleStage { Id = stageId };
        // Child whose parent is the stage (making it a descendant)
        WorkScheduleStage child = new WorkScheduleStage { Id = childId, ParentStageId = stageId };

        _stageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStage>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStage, object>>[]>()))
            .ReturnsAsync(new List<WorkScheduleStage> { stage, child });

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>()
            .WithMessage("*descendant*");
    }
}
