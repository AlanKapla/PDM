using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.WorkSchedules.SyncWorkScheduleWithEstimate;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.WorkSchedules;

public sealed class SyncWorkScheduleWithEstimateCommandHandlerTests
{
    private readonly Mock<IRepository<WorkSchedule>> _workScheduleRepoMock = new();
    private readonly Mock<IWorkScheduleSyncService> _syncServiceMock = new();
    private readonly Mock<ICostEstimateAccessService> _costEstimateAccessServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IWorkScheduleCacheService> _scheduleCacheMock = new();
    private readonly Mock<IWorkScheduleAccessService> _accessServiceMock = new();
    private readonly SyncWorkScheduleWithEstimateCommandHandler _handler;

    public SyncWorkScheduleWithEstimateCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _handler = new SyncWorkScheduleWithEstimateCommandHandler(
            _workScheduleRepoMock.Object,
            _syncServiceMock.Object,
            _costEstimateAccessServiceMock.Object,
            _currentUserMock.Object,
            _scheduleCacheMock.Object,
            _accessServiceMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static SyncWorkScheduleWithEstimateCommand ValidCommand() =>
        new SyncWorkScheduleWithEstimateCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            WorkScheduleId = Guid.NewGuid()
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenWorkScheduleNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        SyncWorkScheduleWithEstimateCommand command = ValidCommand();

        _workScheduleRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<WorkSchedule, bool>>>(),
                It.IsAny<Func<IQueryable<WorkSchedule>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkSchedule, object>>[]>()))
            .ReturnsAsync((WorkSchedule?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenNoCostEstimateLinked_ThrowsValidationApiException()
    {
        // Arrange
        SyncWorkScheduleWithEstimateCommand command = ValidCommand();
        WorkSchedule schedule = new WorkSchedule { Id = command.WorkScheduleId, CostEstimateId = null };

        _workScheduleRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<WorkSchedule, bool>>>(),
                It.IsAny<Func<IQueryable<WorkSchedule>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkSchedule, object>>[]>()))
            .ReturnsAsync(schedule);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>()
            .WithMessage("*cost estimate*");
    }

    [Fact]
    public async Task Handle_WhenInsufficientAccessLevel_ThrowsForbiddenApiException()
    {
        // Arrange
        SyncWorkScheduleWithEstimateCommand command = ValidCommand();
        WorkSchedule schedule = new WorkSchedule
        {
            Id = command.WorkScheduleId,
            CostEstimateId = Guid.NewGuid()
        };

        _workScheduleRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<WorkSchedule, bool>>>(),
                It.IsAny<Func<IQueryable<WorkSchedule>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkSchedule, object>>[]>()))
            .ReturnsAsync(schedule);

        _costEstimateAccessServiceMock
            .Setup(s => s.GetAccessLevelAsync(
                It.IsAny<ICurrentUser>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostEstimateAccessLevel.ReadOnly);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>();
    }

    [Fact]
    public async Task Handle_WhenValidRequest_CallsSyncServiceAndInvalidatesCache()
    {
        // Arrange
        SyncWorkScheduleWithEstimateCommand command = ValidCommand();
        WorkSchedule schedule = new WorkSchedule
        {
            Id = command.WorkScheduleId,
            CostEstimateId = Guid.NewGuid()
        };

        _workScheduleRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<WorkSchedule, bool>>>(),
                It.IsAny<Func<IQueryable<WorkSchedule>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkSchedule, object>>[]>()))
            .ReturnsAsync(schedule);

        _costEstimateAccessServiceMock
            .Setup(s => s.GetAccessLevelAsync(
                It.IsAny<ICurrentUser>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CostEstimateAccessLevel.Full);

        _syncServiceMock
            .Setup(s => s.SyncFromCostEstimateAsync(schedule, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkScheduleStage>());

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _syncServiceMock.Verify(s => s.SyncFromCostEstimateAsync(schedule, It.IsAny<CancellationToken>()), Times.Once);
        _scheduleCacheMock.Verify(c => c.InvalidateScheduleAsync(command.WorkScheduleId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
