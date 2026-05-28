using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;
using CQRS.Projects.GetTenantProjects;
using Entities.Models.Projects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Projects;

public sealed class GetTenantProjectsQueryHandlerTests
{
    private readonly Mock<IReadRepository<Project>> _projectRepoMock = new();
    private readonly Mock<IRepository<ProjectMember>> _projectMemberRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetTenantProjectsQueryHandler _handler;

    public GetTenantProjectsQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(false);
        _currentUserMock
            .Setup(u => u.GetTenantSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantCtxSnapshot?)null);
        _currentUserMock
            .Setup(u => u.GetProjectSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectCtxSnapshot?)null);

        _handler = new GetTenantProjectsQueryHandler(
            _projectRepoMock.Object,
            _projectMemberRepoMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static GetTenantProjectsQuery ValidQuery(Guid tenantId) =>
        new GetTenantProjectsQuery { TenantId = tenantId };

    private static Project BuildProject(Guid tenantId, string name, bool isActive = true) => new Project
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = name,
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow,
        CreatedByUserId = Guid.NewGuid(),
    };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserIsTenantAdmin_ReturnsAllProjects()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        TenantCtxSnapshot tenantSnapshot = new TenantCtxSnapshot(
            TenantId: tenantId,
            IsAdmin: true,
            IsActive: true);

        _currentUserMock
            .Setup(u => u.GetTenantSnapshotAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantSnapshot);

        List<Project> projects =
        [
            BuildProject(tenantId, "Alpha"),
            BuildProject(tenantId, "Beta", isActive: false),
        ];

        _projectRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<Func<IQueryable<Project>, IIncludableQueryable<Project, object>>>()))
            .ReturnsAsync(projects);

        _projectMemberRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<ProjectMember, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectMember>, IIncludableQueryable<ProjectMember, object>>>()))
            .ReturnsAsync(new List<ProjectMember>());

        GetTenantProjectsQuery query = ValidQuery(tenantId);

        // Act
        IEnumerable<ProjectDetailsWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.IsAdmin, "tenant admin should have IsAdmin=true on all projects");
    }

    [Fact]
    public async Task Handle_WhenUserIsTenantAdmin_ProjectHasIsAdminTrue_EvenWithoutExplicitProjectMembership()
    {
        // Arrange — tenant admin with NO project membership
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        TenantCtxSnapshot tenantSnapshot = new TenantCtxSnapshot(
            TenantId: tenantId,
            IsAdmin: true,
            IsActive: true);

        _currentUserMock
            .Setup(u => u.GetTenantSnapshotAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantSnapshot);

        List<Project> projects = [BuildProject(tenantId, "Alpha")];

        _projectRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<Func<IQueryable<Project>, IIncludableQueryable<Project, object>>>()))
            .ReturnsAsync(projects);

        _projectMemberRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<ProjectMember, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectMember>, IIncludableQueryable<ProjectMember, object>>>()))
            .ReturnsAsync(new List<ProjectMember>()); // no project membership

        // Act
        IEnumerable<ProjectDetailsWeb> result = await _handler.Handle(ValidQuery(tenantId), CancellationToken.None);

        // Assert
        ProjectDetailsWeb project = result.Should().ContainSingle().Subject;
        project.IsAdmin.Should().BeTrue("tenant admin should have IsAdmin=true even without explicit project membership");
    }

    [Fact]
    public async Task Handle_WhenNoProjectsExist_ReturnsEmptyList()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();

        _projectRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<Func<IQueryable<Project>, IIncludableQueryable<Project, object>>>()))
            .ReturnsAsync(new List<Project>());

        _projectMemberRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<ProjectMember, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectMember>, IIncludableQueryable<ProjectMember, object>>>()))
            .ReturnsAsync(new List<ProjectMember>());

        GetTenantProjectsQuery query = ValidQuery(tenantId);

        // Act
        IEnumerable<ProjectDetailsWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenUserIsRegularMember_ReturnsOnlyActiveProjects()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        Project activeProject = BuildProject(tenantId, "Active Project", isActive: true);
        Project inactiveProject = BuildProject(tenantId, "Inactive Project", isActive: false);

        _projectRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<Func<IQueryable<Project>, IIncludableQueryable<Project, object>>>()))
            .ReturnsAsync(new List<Project> { activeProject, inactiveProject });

        List<ProjectMember> members =
        [
            new ProjectMember { ProjectId = activeProject.Id, TenantId = tenantId, UserId = userId, IsAdmin = false },
            new ProjectMember { ProjectId = inactiveProject.Id, TenantId = tenantId, UserId = userId, IsAdmin = false },
        ];

        _projectMemberRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<ProjectMember, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectMember>, IIncludableQueryable<ProjectMember, object>>>()))
            .ReturnsAsync(members);

        GetTenantProjectsQuery query = ValidQuery(tenantId);

        // Act
        IEnumerable<ProjectDetailsWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        List<ProjectDetailsWeb> list = result.ToList();
        list.Should().HaveCount(1);
        list[0].Name.Should().Be("Active Project");
    }
}
