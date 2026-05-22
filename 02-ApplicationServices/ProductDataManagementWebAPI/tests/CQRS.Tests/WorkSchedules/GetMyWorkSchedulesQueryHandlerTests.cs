using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.WorkSchedules.GetMyWorkSchedules;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.WorkSchedules;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.WorkSchedules;

public sealed class GetMyWorkSchedulesQueryHandlerTests
{
    private readonly Mock<IRepository<Project>> _projectRepoMock = new();
    private readonly Mock<IRepository<WorkScheduleStageWorkAssignment>> _assignmentRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetMyWorkSchedulesQueryHandler _handler;

    private readonly Guid _currentUserId = Guid.NewGuid();

    public GetMyWorkSchedulesQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_currentUserId);

        _handler = new GetMyWorkSchedulesQueryHandler(
            _projectRepoMock.Object,
            _assignmentRepoMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static GetMyWorkSchedulesQuery ValidQuery() =>
        new GetMyWorkSchedulesQuery
        {
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid()
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenProjectNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        GetMyWorkSchedulesQuery query = ValidQuery();

        _projectRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<Func<IQueryable<Project>, IIncludableQueryable<Project, object>>[]>()))
            .ReturnsAsync((Project?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenNoAssignmentsExist_ReturnsEmptyList()
    {
        // Arrange
        GetMyWorkSchedulesQuery query = ValidQuery();
        Project project = new Project
        {
            Id = query.ProjectId,
            TenantId = query.TenantId,
            Name = "Test Project",
            Tenant = new Tenant { Id = query.TenantId, Name = "Test Tenant" }
        };

        _projectRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<Func<IQueryable<Project>, IIncludableQueryable<Project, object>>[]>()))
            .ReturnsAsync(project);

        _assignmentRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<WorkScheduleStageWorkAssignment, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWorkAssignment>, IIncludableQueryable<WorkScheduleStageWorkAssignment, object>>[]>()))
            .ReturnsAsync(new List<WorkScheduleStageWorkAssignment>());

        // Act
        List<MyWorkSchedulesTenantDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenAssignmentsExist_ReturnsTenantDtoWithWorkSchedules()
    {
        // Arrange
        GetMyWorkSchedulesQuery query = ValidQuery();
        Project project = new Project
        {
            Id = query.ProjectId,
            TenantId = query.TenantId,
            Name = "Test Project",
            Tenant = new Tenant { Id = query.TenantId, Name = "Test Tenant" }
        };

        Guid workScheduleId = Guid.NewGuid();
        WorkSchedule workSchedule = new WorkSchedule { Id = workScheduleId, Name = "My WS", IsDeleted = false };
        WorkScheduleStage stage = new WorkScheduleStage { Id = Guid.NewGuid(), WorkSchedule = workSchedule };
        WorkScheduleStageWork work = new WorkScheduleStageWork { Id = Guid.NewGuid(), Stage = stage };
        WorkScheduleStageWorkAssignment assignment = new WorkScheduleStageWorkAssignment
        {
            WorkScheduleStageWorkId = work.Id,
            TenantId = query.TenantId,
            ProjectId = query.ProjectId,
            UserId = _currentUserId,
            Work = work
        };

        _projectRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<Func<IQueryable<Project>, IIncludableQueryable<Project, object>>[]>()))
            .ReturnsAsync(project);

        _assignmentRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<WorkScheduleStageWorkAssignment, bool>>>(),
                It.IsAny<Func<IQueryable<WorkScheduleStageWorkAssignment>, IIncludableQueryable<WorkScheduleStageWorkAssignment, object>>[]>()))
            .ReturnsAsync(new List<WorkScheduleStageWorkAssignment> { assignment });

        // Act
        List<MyWorkSchedulesTenantDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].TenantName.Should().Be("Test Tenant");
        result[0].Projects[0].WorkSchedules.Should().HaveCount(1);
        result[0].Projects[0].WorkSchedules[0].WorkScheduleId.Should().Be(workScheduleId);
    }
}
