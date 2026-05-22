using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using CQRS.WorkSchedules.SetWorkScheduleStageWorkColorRgb;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.WorkSchedules;

public sealed class SetWorkScheduleStageWorkColorRgbCommandHandlerTests
{
    private readonly Mock<IRepository<WorkScheduleStageWork>> _workRepoMock = new();
    private readonly Mock<IWorkScheduleCacheService> _scheduleCacheMock = new();
    private readonly Mock<IWorkScheduleAccessService> _accessServiceMock = new();
    private readonly SetWorkScheduleStageWorkColorRgbCommandHandler _handler;

    public SetWorkScheduleStageWorkColorRgbCommandHandlerTests()
    {
        _handler = new SetWorkScheduleStageWorkColorRgbCommandHandler(
            _workRepoMock.Object,
            _scheduleCacheMock.Object,
            _accessServiceMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static SetWorkScheduleStageWorkColorRgbCommand ValidCommand() =>
        new SetWorkScheduleStageWorkColorRgbCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            WorkScheduleId = Guid.NewGuid(),
            WorkScheduleStageId = Guid.NewGuid(),
            WorkScheduleStageWorkId = Guid.NewGuid(),
            ColorRgb = "#FF5500"
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenWorkExists_UpdatesColorRgbAndInvalidatesCache()
    {
        // Arrange
        SetWorkScheduleStageWorkColorRgbCommand command = ValidCommand();
        WorkScheduleStageWork work = new WorkScheduleStageWork
        {
            Id = command.WorkScheduleStageWorkId,
            ColorRgb = "#000000"
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
        work.ColorRgb.Should().Be("#FF5500");
        _workRepoMock.Verify(r => r.Update(work), Times.Once);
        _workRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _scheduleCacheMock.Verify(c => c.InvalidateScheduleAsync(command.WorkScheduleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenWorkNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        SetWorkScheduleStageWorkColorRgbCommand command = ValidCommand();

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
    public async Task Handle_WhenWorkExists_DoesNotCallAccessServiceAfterWorkIsFound()
    {
        // Arrange
        SetWorkScheduleStageWorkColorRgbCommand command = ValidCommand();
        WorkScheduleStageWork work = new WorkScheduleStageWork { Id = command.WorkScheduleStageWorkId };

        _workRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWork>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWork, object>>[]>()))
            .ReturnsAsync(work);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _accessServiceMock.Verify(
            a => a.RequireAdminOrOwnerAsync(command.TenantId, command.ProjectId, command.WorkScheduleId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
