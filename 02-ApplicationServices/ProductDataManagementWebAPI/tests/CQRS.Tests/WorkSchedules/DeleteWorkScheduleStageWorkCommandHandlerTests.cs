using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using CQRS.WorkSchedules.DeleteWorkScheduleStageWork;
using Entities.Models.CostTrackers;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.WorkSchedules;

public sealed class DeleteWorkScheduleStageWorkCommandHandlerTests
{
    private readonly Mock<IRepository<WorkScheduleStageWork>> _workRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWorkDependency>> _dependencyRepoMock = new();
    private readonly Mock<IRepository<TrackedCost>> _trackedCostRepoMock = new();
    private readonly Mock<IWorkScheduleCacheService> _scheduleCacheMock = new();
    private readonly Mock<IWorkScheduleAccessService> _accessServiceMock = new();
    private readonly DeleteWorkScheduleStageWorkCommandHandler _handler;

    public DeleteWorkScheduleStageWorkCommandHandlerTests()
    {
        _handler = new DeleteWorkScheduleStageWorkCommandHandler(
            _workRepoMock.Object,
            _dependencyRepoMock.Object,
            _trackedCostRepoMock.Object,
            _scheduleCacheMock.Object,
            _accessServiceMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static DeleteWorkScheduleStageWorkCommand ValidCommand() =>
        new DeleteWorkScheduleStageWorkCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            WorkScheduleId = Guid.NewGuid(),
            WorkScheduleStageId = Guid.NewGuid(),
            WorkScheduleStageWorkId = Guid.NewGuid()
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenWorkExists_ExecutesDeletePipelineAndInvalidatesCache()
    {
        // Arrange
        DeleteWorkScheduleStageWorkCommand command = ValidCommand();

        _workRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _trackedCostRepoMock
            .Setup(r => r.ExecuteUpdateAsync(
                It.IsAny<Expression<Func<TrackedCost, bool>>>(),
                It.IsAny<Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<TrackedCost>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _workRepoMock
            .Setup(r => r.ExecuteUpdateAsync(
                It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<WorkScheduleStageWork>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _dependencyRepoMock
            .Setup(r => r.ExecuteDeleteAsync(
                It.IsAny<Expression<Func<WorkScheduleStageWorkDependency, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _scheduleCacheMock.Verify(c => c.InvalidateScheduleAsync(command.WorkScheduleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenWorkNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        DeleteWorkScheduleStageWorkCommand command = ValidCommand();

        _workRepoMock
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
