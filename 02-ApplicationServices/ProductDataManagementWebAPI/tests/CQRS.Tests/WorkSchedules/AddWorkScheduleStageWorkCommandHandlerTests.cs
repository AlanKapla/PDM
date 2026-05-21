using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using CQRS.WorkSchedules.AddWorkScheduleStageWork;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.WorkSchedules;

public sealed class AddWorkScheduleStageWorkCommandHandlerTests
{
    private readonly Mock<IRepository<WorkScheduleStage>> _stageRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWork>> _workRepoMock = new();
    private readonly Mock<IWorkScheduleCacheService> _scheduleCacheMock = new();
    private readonly Mock<IWorkScheduleAccessService> _accessServiceMock = new();
    private readonly AddWorkScheduleStageWorkCommandHandler _handler;

    public AddWorkScheduleStageWorkCommandHandlerTests()
    {
        _handler = new AddWorkScheduleStageWorkCommandHandler(
            _stageRepoMock.Object,
            _workRepoMock.Object,
            _scheduleCacheMock.Object,
            _accessServiceMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static AddWorkScheduleStageWorkCommand ValidCommand() =>
        new AddWorkScheduleStageWorkCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            WorkScheduleId = Guid.NewGuid(),
            WorkScheduleStageId = Guid.NewGuid(),
            Name = "Work Item 1",
            Order = 0,
            ColorRgb = "#FF0000"
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenStageExists_InsertsWorkAndReturnsId()
    {
        // Arrange
        AddWorkScheduleStageWorkCommand command = ValidCommand();

        _stageRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Guid result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _workRepoMock.Verify(r => r.Insert(It.IsAny<WorkScheduleStageWork>()), Times.Once);
        _workRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _scheduleCacheMock.Verify(c => c.InvalidateScheduleAsync(command.WorkScheduleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenStageNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        AddWorkScheduleStageWorkCommand command = ValidCommand();

        _stageRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<WorkScheduleStage, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }
}
