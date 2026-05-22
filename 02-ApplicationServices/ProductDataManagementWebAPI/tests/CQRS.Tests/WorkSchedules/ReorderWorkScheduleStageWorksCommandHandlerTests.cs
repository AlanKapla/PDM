using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using CQRS.WorkSchedules.ReorderWorkScheduleStageWorks;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.WorkSchedules;

public sealed class ReorderWorkScheduleStageWorksCommandHandlerTests
{
    private readonly Mock<IRepository<WorkScheduleStageWork>> _workRepoMock = new();
    private readonly Mock<IWorkScheduleCacheService> _scheduleCacheMock = new();
    private readonly Mock<IWorkScheduleAccessService> _accessServiceMock = new();
    private readonly ReorderWorkScheduleStageWorksCommandHandler _handler;

    public ReorderWorkScheduleStageWorksCommandHandlerTests()
    {
        _handler = new ReorderWorkScheduleStageWorksCommandHandler(
            _workRepoMock.Object,
            _scheduleCacheMock.Object,
            _accessServiceMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ReorderWorkScheduleStageWorksCommand ValidCommand(List<Guid> orderedIds) =>
        new ReorderWorkScheduleStageWorksCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            WorkScheduleId = Guid.NewGuid(),
            WorkScheduleStageId = Guid.NewGuid(),
            OrderedWorkIds = orderedIds
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenOrderedIdsMatchAllWorks_UpdatesOrderAndInvalidatesCache()
    {
        // Arrange
        Guid id1 = Guid.NewGuid();
        Guid id2 = Guid.NewGuid();
        List<Guid> orderedIds = new List<Guid> { id2, id1 };
        ReorderWorkScheduleStageWorksCommand command = ValidCommand(orderedIds);

        List<WorkScheduleStageWork> works = new List<WorkScheduleStageWork>
        {
            new WorkScheduleStageWork { Id = id1, Order = 0 },
            new WorkScheduleStageWork { Id = id2, Order = 1 }
        };

        _workRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWork>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWork, object>>[]>()))
            .ReturnsAsync(works);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _workRepoMock.Verify(r => r.UpdateRange(It.IsAny<IEnumerable<WorkScheduleStageWork>>()), Times.Once);
        _workRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _scheduleCacheMock.Verify(c => c.InvalidateScheduleAsync(command.WorkScheduleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrderedIdsMismatch_ThrowsValidationApiException()
    {
        // Arrange
        Guid id1 = Guid.NewGuid();
        Guid id2 = Guid.NewGuid();
        Guid unknownId = Guid.NewGuid();
        List<Guid> orderedIds = new List<Guid> { id1, unknownId };
        ReorderWorkScheduleStageWorksCommand command = ValidCommand(orderedIds);

        List<WorkScheduleStageWork> works = new List<WorkScheduleStageWork>
        {
            new WorkScheduleStageWork { Id = id1, Order = 0 },
            new WorkScheduleStageWork { Id = id2, Order = 1 }
        };

        _workRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWork>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWork, object>>[]>()))
            .ReturnsAsync(works);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>();
    }

    [Fact]
    public async Task Handle_WhenAccessServiceCalled_RequiresAdminOrOwner()
    {
        // Arrange
        Guid id1 = Guid.NewGuid();
        List<Guid> orderedIds = new List<Guid> { id1 };
        ReorderWorkScheduleStageWorksCommand command = ValidCommand(orderedIds);

        List<WorkScheduleStageWork> works = new List<WorkScheduleStageWork>
        {
            new WorkScheduleStageWork { Id = id1, Order = 0 }
        };

        _workRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWork>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWork, object>>[]>()))
            .ReturnsAsync(works);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _accessServiceMock.Verify(
            a => a.RequireAdminOrOwnerAsync(command.TenantId, command.ProjectId, command.WorkScheduleId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
