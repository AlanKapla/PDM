using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using CQRS.WorkSchedules.RenameWorkScheduleStage;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.WorkSchedules;

public sealed class RenameWorkScheduleStageCommandHandlerTests
{
    private readonly Mock<IRepository<WorkScheduleStage>> _stageRepoMock = new();
    private readonly Mock<IWorkScheduleCacheService> _scheduleCacheMock = new();
    private readonly Mock<IWorkScheduleAccessService> _accessServiceMock = new();
    private readonly RenameWorkScheduleStageCommandHandler _handler;

    public RenameWorkScheduleStageCommandHandlerTests()
    {
        _handler = new RenameWorkScheduleStageCommandHandler(
            _stageRepoMock.Object,
            _scheduleCacheMock.Object,
            _accessServiceMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static RenameWorkScheduleStageCommand ValidCommand() =>
        new RenameWorkScheduleStageCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            WorkScheduleId = Guid.NewGuid(),
            WorkScheduleStageId = Guid.NewGuid(),
            Name = "Renamed Stage"
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenStageExists_RenamesAndSavesChanges()
    {
        // Arrange
        RenameWorkScheduleStageCommand command = ValidCommand();
        WorkScheduleStage stage = new WorkScheduleStage
        {
            Id = command.WorkScheduleStageId,
            TenantId = command.TenantId,
            WorkScheduleId = command.WorkScheduleId,
            Name = "Old Name"
        };

        _stageRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<WorkScheduleStage, bool>>>()))
            .ReturnsAsync(stage);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        stage.Name.Should().Be("Renamed Stage");
        _stageRepoMock.Verify(r => r.Update(stage), Times.Once);
        _stageRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _scheduleCacheMock.Verify(c => c.InvalidateScheduleAsync(command.WorkScheduleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenStageNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        RenameWorkScheduleStageCommand command = ValidCommand();

        _stageRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<WorkScheduleStage, bool>>>()))
            .ReturnsAsync((WorkScheduleStage?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }
}
