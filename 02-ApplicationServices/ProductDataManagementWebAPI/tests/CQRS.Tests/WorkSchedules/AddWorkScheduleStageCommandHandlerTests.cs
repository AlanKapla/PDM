using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using CQRS.WorkSchedules.AddWorkScheduleStage;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.WorkSchedules;

public sealed class AddWorkScheduleStageCommandHandlerTests
{
    private readonly Mock<IRepository<WorkSchedule>> _workScheduleRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStage>> _stageRepoMock = new();
    private readonly Mock<IWorkScheduleCacheService> _scheduleCacheMock = new();
    private readonly Mock<IWorkScheduleAccessService> _accessServiceMock = new();
    private readonly AddWorkScheduleStageCommandHandler _handler;

    public AddWorkScheduleStageCommandHandlerTests()
    {
        _handler = new AddWorkScheduleStageCommandHandler(
            _workScheduleRepoMock.Object,
            _stageRepoMock.Object,
            _scheduleCacheMock.Object,
            _accessServiceMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static AddWorkScheduleStageCommand ValidCommand() =>
        new AddWorkScheduleStageCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            WorkScheduleId = Guid.NewGuid(),
            Name = "Stage 1",
            Order = 0
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenWorkScheduleExists_InsertsStageAndReturnsId()
    {
        // Arrange
        AddWorkScheduleStageCommand command = ValidCommand();

        _workScheduleRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<WorkSchedule, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Guid result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _stageRepoMock.Verify(r => r.Insert(It.IsAny<WorkScheduleStage>()), Times.Once);
        _stageRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _scheduleCacheMock.Verify(c => c.InvalidateScheduleAsync(command.WorkScheduleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenWorkScheduleNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        AddWorkScheduleStageCommand command = ValidCommand();

        _workScheduleRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<WorkSchedule, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }
}
