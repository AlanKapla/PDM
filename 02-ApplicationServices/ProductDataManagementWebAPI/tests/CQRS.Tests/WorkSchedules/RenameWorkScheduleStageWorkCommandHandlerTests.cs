using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using CQRS.WorkSchedules.RenameWorkScheduleStageWork;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.WorkSchedules;

public sealed class RenameWorkScheduleStageWorkCommandHandlerTests
{
    private readonly Mock<IRepository<WorkScheduleStageWork>> _workRepoMock = new();
    private readonly Mock<IWorkScheduleCacheService> _scheduleCacheMock = new();
    private readonly Mock<IWorkScheduleAccessService> _accessServiceMock = new();
    private readonly RenameWorkScheduleStageWorkCommandHandler _handler;

    public RenameWorkScheduleStageWorkCommandHandlerTests()
    {
        _handler = new RenameWorkScheduleStageWorkCommandHandler(
            _workRepoMock.Object,
            _scheduleCacheMock.Object,
            _accessServiceMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static RenameWorkScheduleStageWorkCommand ValidCommand() =>
        new RenameWorkScheduleStageWorkCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            WorkScheduleId = Guid.NewGuid(),
            WorkScheduleStageId = Guid.NewGuid(),
            WorkScheduleStageWorkId = Guid.NewGuid(),
            Name = "Renamed Work"
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenWorkExists_RenamesAndSavesChanges()
    {
        // Arrange
        RenameWorkScheduleStageWorkCommand command = ValidCommand();
        WorkScheduleStageWork work = new WorkScheduleStageWork
        {
            Id = command.WorkScheduleStageWorkId,
            TenantId = command.TenantId,
            ProjectId = command.ProjectId,
            WorkScheduleStageId = command.WorkScheduleStageId,
            Name = "Old Name",
            ColorRgb = "#000000"
        };

        _workRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>()))
            .ReturnsAsync(work);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        work.Name.Should().Be("Renamed Work");
        _workRepoMock.Verify(r => r.Update(work), Times.Once);
        _workRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _scheduleCacheMock.Verify(c => c.InvalidateScheduleAsync(command.WorkScheduleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenWorkNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        RenameWorkScheduleStageWorkCommand command = ValidCommand();

        _workRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>()))
            .ReturnsAsync((WorkScheduleStageWork?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }
}
