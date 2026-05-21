using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.WorkSchedules.CreateWorkSchedule;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.WorkSchedules;

public sealed class CreateWorkScheduleCommandHandlerTests
{
    private readonly Mock<IRepository<WorkSchedule>> _workScheduleRepoMock = new();
    private readonly Mock<IWorkScheduleSyncService> _syncServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly CreateWorkScheduleCommandHandler _handler;

    public CreateWorkScheduleCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _handler = new CreateWorkScheduleCommandHandler(
            _workScheduleRepoMock.Object,
            _syncServiceMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static CreateWorkScheduleCommand ValidCommand(Guid? costEstimateId = null) =>
        new CreateWorkScheduleCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Name = "Test Schedule",
            CostEstimateId = costEstimateId
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenCommandIsValid_InsertsWorkScheduleAndReturnsId()
    {
        // Arrange
        CreateWorkScheduleCommand command = ValidCommand();

        // Act
        Guid result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _workScheduleRepoMock.Verify(r => r.Insert(It.IsAny<WorkSchedule>()), Times.Once);
        _workScheduleRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCostEstimateIdIsProvided_CallsSyncService()
    {
        // Arrange
        Guid costEstimateId = Guid.NewGuid();
        CreateWorkScheduleCommand command = ValidCommand(costEstimateId);

        _syncServiceMock
            .Setup(s => s.SyncFromCostEstimateAsync(It.IsAny<WorkSchedule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkScheduleStage>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _syncServiceMock.Verify(
            s => s.SyncFromCostEstimateAsync(It.IsAny<WorkSchedule>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCostEstimateIdIsNull_DoesNotCallSyncService()
    {
        // Arrange
        CreateWorkScheduleCommand command = ValidCommand(null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _syncServiceMock.Verify(
            s => s.SyncFromCostEstimateAsync(It.IsAny<WorkSchedule>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
