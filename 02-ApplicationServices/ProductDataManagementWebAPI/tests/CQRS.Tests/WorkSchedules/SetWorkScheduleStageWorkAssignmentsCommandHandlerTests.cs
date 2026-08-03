using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using CQRS.WorkSchedules.SetWorkScheduleStageWorkAssignments;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.WorkSchedules;

public sealed class SetWorkScheduleStageWorkAssignmentsCommandHandlerTests
{
    private readonly Mock<IRepository<WorkScheduleStageWork>> _workRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWorkAssignment>> _assignmentRepoMock = new();
    private readonly Mock<IRepository<WorkSchedule>> _workScheduleRepoMock = new();
    private readonly Mock<IProjectMemberService> _projectMemberServiceMock = new();
    private readonly Mock<IContractorService> _contractorServiceMock = new();
    private readonly Mock<IWorkScheduleNotificationService> _notificationServiceMock = new();
    private readonly Mock<IWorkScheduleCacheService> _scheduleCacheMock = new();
    private readonly Mock<IWorkScheduleAccessService> _accessServiceMock = new();
    private readonly SetWorkScheduleStageWorkAssignmentsCommandHandler _handler;

    public SetWorkScheduleStageWorkAssignmentsCommandHandlerTests()
    {
        _handler = new SetWorkScheduleStageWorkAssignmentsCommandHandler(
            _workRepoMock.Object,
            _assignmentRepoMock.Object,
            _workScheduleRepoMock.Object,
            _projectMemberServiceMock.Object,
            _contractorServiceMock.Object,
            _notificationServiceMock.Object,
            _scheduleCacheMock.Object,
            _accessServiceMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static SetWorkScheduleStageWorkAssignmentsCommand ValidCommand(List<Guid>? userIds = null) =>
        new SetWorkScheduleStageWorkAssignmentsCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            WorkScheduleId = Guid.NewGuid(),
            WorkScheduleStageWorkId = Guid.NewGuid(),
            UserIds = userIds ?? new List<Guid>()
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenWorkNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        SetWorkScheduleStageWorkAssignmentsCommand command = ValidCommand();

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

    [Fact]
    public async Task Handle_WhenUsersAreNotProjectMembers_ThrowsValidationApiException()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        SetWorkScheduleStageWorkAssignmentsCommand command = ValidCommand(new List<Guid> { userId });

        _workRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _projectMemberServiceMock
            .Setup(s => s.AreAllMembersOfProjectAsync(
                command.ProjectId,
                It.IsAny<List<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>();
    }

    [Fact]
    public async Task Handle_WhenEmptyUserIds_SkipsProjectMemberCheck()
    {
        // Arrange
        SetWorkScheduleStageWorkAssignmentsCommand command = ValidCommand(new List<Guid>());

        _workRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _assignmentRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<WorkScheduleStageWorkAssignment, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWorkAssignment>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<WorkScheduleStageWorkAssignment, object>>[]>()))
            .ReturnsAsync(new List<WorkScheduleStageWorkAssignment>());

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _projectMemberServiceMock.Verify(
            s => s.AreAllMembersOfProjectAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _scheduleCacheMock.Verify(
            c => c.InvalidateWorkAsync(command.WorkScheduleId, command.WorkScheduleStageWorkId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
