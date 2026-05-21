using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using CQRS.WorkSchedules.ReorderWorkScheduleStages;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.WorkSchedules;

public sealed class ReorderWorkScheduleStagesCommandHandlerTests
{
    private readonly Mock<IRepository<WorkScheduleStage>> _stageRepoMock = new();
    private readonly Mock<IWorkScheduleCacheService> _scheduleCacheMock = new();
    private readonly Mock<IWorkScheduleAccessService> _accessServiceMock = new();
    private readonly ReorderWorkScheduleStagesCommandHandler _handler;

    public ReorderWorkScheduleStagesCommandHandlerTests()
    {
        _handler = new ReorderWorkScheduleStagesCommandHandler(
            _stageRepoMock.Object,
            _scheduleCacheMock.Object,
            _accessServiceMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ReorderWorkScheduleStagesCommand ValidCommand(List<Guid> orderedIds) =>
        new ReorderWorkScheduleStagesCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            WorkScheduleId = Guid.NewGuid(),
            OrderedStageIds = orderedIds
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenOrderedIdsMatchAllStages_UpdatesOrderAndInvalidatesCache()
    {
        // Arrange
        Guid id1 = Guid.NewGuid();
        Guid id2 = Guid.NewGuid();
        List<Guid> orderedIds = new List<Guid> { id2, id1 };
        ReorderWorkScheduleStagesCommand command = ValidCommand(orderedIds);

        List<WorkScheduleStage> stages = new List<WorkScheduleStage>
        {
            new WorkScheduleStage { Id = id1, Order = 0 },
            new WorkScheduleStage { Id = id2, Order = 1 }
        };

        _stageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStage>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStage, object>>[]>()))
            .ReturnsAsync(stages);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _stageRepoMock.Verify(r => r.UpdateRange(It.IsAny<IEnumerable<WorkScheduleStage>>()), Times.Once);
        _stageRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _scheduleCacheMock.Verify(c => c.InvalidateScheduleAsync(command.WorkScheduleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrderedIdsMismatch_ThrowsValidationApiException()
    {
        // Arrange
        Guid id1 = Guid.NewGuid();
        Guid id2 = Guid.NewGuid();
        Guid unknownId = Guid.NewGuid();
        // orderedIds contains an ID not in the actual stages
        List<Guid> orderedIds = new List<Guid> { id1, unknownId };
        ReorderWorkScheduleStagesCommand command = ValidCommand(orderedIds);

        List<WorkScheduleStage> stages = new List<WorkScheduleStage>
        {
            new WorkScheduleStage { Id = id1, Order = 0 },
            new WorkScheduleStage { Id = id2, Order = 1 }
        };

        _stageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStage>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStage, object>>[]>()))
            .ReturnsAsync(stages);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>();
    }

    [Fact]
    public async Task Handle_WhenOrderedIdsHaveDuplicates_ThrowsValidationApiException()
    {
        // Arrange
        Guid id1 = Guid.NewGuid();
        // Duplicate id1 in ordered list
        List<Guid> orderedIds = new List<Guid> { id1, id1 };
        ReorderWorkScheduleStagesCommand command = ValidCommand(orderedIds);

        List<WorkScheduleStage> stages = new List<WorkScheduleStage>
        {
            new WorkScheduleStage { Id = id1, Order = 0 },
            new WorkScheduleStage { Id = Guid.NewGuid(), Order = 1 }
        };

        _stageRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStage>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStage, object>>[]>()))
            .ReturnsAsync(stages);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>();
    }
}
