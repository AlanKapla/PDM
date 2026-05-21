using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using CQRS.WorkSchedules.UpdateWorkSchedule;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.WorkSchedules;

public sealed class UpdateWorkScheduleCommandHandlerTests
{
    private readonly Mock<IRepository<WorkSchedule>> _workScheduleRepoMock = new();
    private readonly Mock<IWorkScheduleCacheService> _scheduleCacheMock = new();
    private readonly Mock<IWorkScheduleAccessService> _accessServiceMock = new();
    private readonly UpdateWorkScheduleCommandHandler _handler;

    public UpdateWorkScheduleCommandHandlerTests()
    {
        _handler = new UpdateWorkScheduleCommandHandler(
            _workScheduleRepoMock.Object,
            _scheduleCacheMock.Object,
            _accessServiceMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static UpdateWorkScheduleCommand ValidCommand(Guid? workScheduleId = null) =>
        new UpdateWorkScheduleCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            WorkScheduleId = workScheduleId ?? Guid.NewGuid(),
            Name = "Updated Schedule"
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenWorkScheduleExists_UpdatesNameAndInvalidatesCache()
    {
        // Arrange
        UpdateWorkScheduleCommand command = ValidCommand();
        WorkSchedule workSchedule = new WorkSchedule
        {
            Id = command.WorkScheduleId,
            TenantId = command.TenantId,
            ProjectId = command.ProjectId,
            Name = "Old Name"
        };

        _workScheduleRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<WorkSchedule, bool>>>()))
            .ReturnsAsync(workSchedule);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        workSchedule.Name.Should().Be("Updated Schedule");
        _workScheduleRepoMock.Verify(r => r.Update(workSchedule), Times.Once);
        _workScheduleRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _scheduleCacheMock.Verify(c => c.InvalidateScheduleAsync(command.WorkScheduleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenWorkScheduleNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        UpdateWorkScheduleCommand command = ValidCommand();

        _workScheduleRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<WorkSchedule, bool>>>()))
            .ReturnsAsync((WorkSchedule?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }
}
