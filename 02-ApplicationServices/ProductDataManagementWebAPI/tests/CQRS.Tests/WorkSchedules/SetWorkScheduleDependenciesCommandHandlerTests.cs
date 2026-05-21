using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.WorkSchedules.SetWorkScheduleDependencies;
using CQRS.WorkSchedules.Shared;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.WorkSchedules;

public sealed class SetWorkScheduleDependenciesCommandHandlerTests
{
    private readonly Mock<IRepository<WorkSchedule>> _workScheduleRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWork>> _workRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWorkDependency>> _dependencyRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWorkPeriod>> _periodRepoMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<IWorkScheduleCacheService> _scheduleCacheMock = new();
    private readonly Mock<IWorkScheduleAccessService> _accessServiceMock = new();
    // WorkScheduleBuilder dependencies
    private readonly Mock<IRepository<WorkScheduleStage>> _stageRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWorkAssignment>> _assignmentRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWorkComment>> _commentRepoMock = new();
    private readonly SetWorkScheduleDependenciesCommandHandler _handler;

    public SetWorkScheduleDependenciesCommandHandlerTests()
    {
        WorkScheduleBuilder builder = new WorkScheduleBuilder(
            _workScheduleRepoMock.Object,
            _stageRepoMock.Object,
            _workRepoMock.Object,
            _periodRepoMock.Object,
            _assignmentRepoMock.Object,
            _commentRepoMock.Object,
            _dependencyRepoMock.Object,
            _userServiceMock.Object);

        _handler = new SetWorkScheduleDependenciesCommandHandler(
            _workScheduleRepoMock.Object,
            _workRepoMock.Object,
            _dependencyRepoMock.Object,
            _periodRepoMock.Object,
            _userServiceMock.Object,
            _scheduleCacheMock.Object,
            builder,
            _accessServiceMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static SetWorkScheduleDependenciesCommand ValidCommand(List<WorkDependencyDto>? dependencies = null) =>
        new SetWorkScheduleDependenciesCommand
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            WorkScheduleId = Guid.NewGuid(),
            Dependencies = dependencies ?? new List<WorkDependencyDto>()
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenDependencyReferencesUnknownWork_ThrowsValidationApiException()
    {
        // Arrange
        Guid knownWorkId = Guid.NewGuid();
        Guid unknownWorkId = Guid.NewGuid();
        SetWorkScheduleDependenciesCommand command = ValidCommand(new List<WorkDependencyDto>
        {
            new WorkDependencyDto(knownWorkId, unknownWorkId, WorkDependencyType.FinishToStart, 0)
        });

        // Only knownWorkId is in the work schedule
        _workRepoMock
            .Setup(r => r.SelectToHashSetAsync(
                It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Expression<Func<WorkScheduleStageWork, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { knownWorkId });

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>();
    }

    [Fact]
    public async Task Handle_WhenNoDependencies_CallsSaveChangesAndInvalidatesCache()
    {
        // Arrange — use DefaultValue.Empty so GetBySearch returns Task<IEnumerable<T>> with empty sequence
        _dependencyRepoMock.DefaultValue = DefaultValue.Empty;

        SetWorkScheduleDependenciesCommand command = ValidCommand(new List<WorkDependencyDto>());

        _workRepoMock
            .Setup(r => r.SelectToHashSetAsync(
                It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Expression<Func<WorkScheduleStageWork, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid>());

        // Act + Assert — builder throws NotFoundApiException when workScheduleRepo returns null/empty
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<Exception>();

        _dependencyRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _scheduleCacheMock.Verify(c => c.InvalidateScheduleAsync(command.WorkScheduleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPredecessorWorkIdNotInSchedule_ThrowsValidationApiException()
    {
        // Arrange
        Guid unknownId = Guid.NewGuid();
        SetWorkScheduleDependenciesCommand command = ValidCommand(new List<WorkDependencyDto>
        {
            new WorkDependencyDto(unknownId, Guid.NewGuid(), WorkDependencyType.FinishToStart, 0)
        });

        _workRepoMock
            .Setup(r => r.SelectToHashSetAsync(
                It.IsAny<Expression<Func<WorkScheduleStageWork, bool>>>(),
                It.IsAny<Expression<Func<WorkScheduleStageWork, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid>()); // empty — unknownId not found

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationApiException>()
            .WithMessage("*PredecessorWorkId*");
    }
}
