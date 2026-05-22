using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.WorkSchedules.GetWorkSchedule;
using CQRS.WorkSchedules.Shared;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;

namespace CQRS.Tests.WorkSchedules;

public sealed class GetWorkScheduleQueryHandlerTests
{
    private readonly Mock<IWorkScheduleCacheService> _scheduleCacheMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IWorkScheduleAccessService> _accessServiceMock = new();
    private readonly GetWorkScheduleQueryHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();

    public GetWorkScheduleQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());
        _currentUserMock.Setup(u => u.ActiveTenantId).Returns(_tenantId);
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(true);

        // Build a real WorkScheduleBuilder with mocked repos — it won't be called because the cache mock intercepts
        Mock<IRepository<WorkSchedule>> builderWorkScheduleRepoMock = new();
        Mock<IRepository<WorkScheduleStage>> builderStageRepoMock = new();
        Mock<IRepository<WorkScheduleStageWork>> builderWorkRepoMock = new();
        Mock<IRepository<WorkScheduleStageWorkPeriod>> builderPeriodRepoMock = new();
        Mock<IRepository<WorkScheduleStageWorkAssignment>> builderAssignmentRepoMock = new();
        Mock<IRepository<WorkScheduleStageWorkComment>> builderCommentRepoMock = new();
        Mock<IRepository<WorkScheduleStageWorkDependency>> builderDependencyRepoMock = new();
        Mock<IUserService> builderUserServiceMock = new();

        WorkScheduleBuilder scheduleBuilder = new WorkScheduleBuilder(
            builderWorkScheduleRepoMock.Object,
            builderStageRepoMock.Object,
            builderWorkRepoMock.Object,
            builderPeriodRepoMock.Object,
            builderAssignmentRepoMock.Object,
            builderCommentRepoMock.Object,
            builderDependencyRepoMock.Object,
            builderUserServiceMock.Object);

        _handler = new GetWorkScheduleQueryHandler(
            _scheduleCacheMock.Object,
            scheduleBuilder,
            _currentUserMock.Object,
            _accessServiceMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private GetWorkScheduleQuery ValidQuery(Guid? tenantId = null) =>
        new GetWorkScheduleQuery
        {
            TenantId = tenantId ?? _tenantId,
            ProjectId = Guid.NewGuid(),
            WorkScheduleId = Guid.NewGuid()
        };

    private static WorkScheduleDetailsWeb BuildScheduleDetails(Guid id, Guid tenantId) =>
        new WorkScheduleDetailsWeb(
            Id: id,
            TenantId: tenantId,
            ProjectId: Guid.NewGuid(),
            CostEstimateId: null,
            Name: "Test Schedule",
            CreatedAt: DateTime.UtcNow,
            CreatedByUserId: Guid.NewGuid(),
            CreatedByUserName: "Test User",
            Stages: new List<WorkScheduleStageWeb>(),
            Dependencies: new List<WorkScheduleWorkDependencyWeb>());

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenScheduleExistsInCacheAndSuperAdmin_ReturnsScheduleDetails()
    {
        // Arrange
        GetWorkScheduleQuery query = ValidQuery();
        WorkScheduleDetailsWeb details = BuildScheduleDetails(query.WorkScheduleId, _tenantId);

        _scheduleCacheMock
            .Setup(c => c.GetOrBuildScheduleAsync(
                query.WorkScheduleId,
                It.IsAny<Func<Task<WorkScheduleDetailsWeb>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);

        // Act
        WorkScheduleDetailsWeb result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(query.WorkScheduleId);
    }

    [Fact]
    public async Task Handle_WhenTenantIdDoesNotMatchCurrentUser_ThrowsForbiddenApiException()
    {
        // Arrange
        Guid differentTenantId = Guid.NewGuid();
        GetWorkScheduleQuery query = ValidQuery(differentTenantId);

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenApiException>();
    }

    [Fact]
    public async Task Handle_WhenScheduleNotFoundInCache_ThrowsNotFoundApiException()
    {
        // Arrange
        GetWorkScheduleQuery query = ValidQuery();

        _scheduleCacheMock
            .Setup(c => c.GetOrBuildScheduleAsync(
                query.WorkScheduleId,
                It.IsAny<Func<Task<WorkScheduleDetailsWeb>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkScheduleDetailsWeb?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }
}
