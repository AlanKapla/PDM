using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using CQRS.Projects.GetProjectsDictionary;
using Entities.Models.Projects;
using Entities.Models.Roles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Projects;

public sealed class GetProjectsDictionaryQueryHandlerTests
{
    private readonly Mock<IReadRepository<Project>> _projectRepoMock = new();
    private readonly Mock<IRepository<ProjectMember>> _projectMemberRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetProjectsDictionaryQueryHandler _handler;

    public GetProjectsDictionaryQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());
        _currentUserMock
            .Setup(u => u.GetActiveTenantSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantCtxSnapshot?)null);

        _handler = new GetProjectsDictionaryQueryHandler(
            _projectRepoMock.Object,
            _projectMemberRepoMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static GetProjectsDictionaryQuery ValidQuery(Guid tenantId) =>
        new GetProjectsDictionaryQuery { TenantId = tenantId };

    private static Project BuildProject(Guid tenantId, string name, bool isActive = true) => new Project
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = name,
        IsActive = isActive,
    };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserIsTenantAdmin_ReturnsAllProjectsAsDictionary()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();

        TenantCtxSnapshot snapshot = new TenantCtxSnapshot(
            TenantId: tenantId,
            TenantRoleId: Guid.NewGuid(),
            TenantPermissionCodes: new HashSet<string>(),
            IsTenantAdmin: true,
            IsActive: true);

        _currentUserMock
            .Setup(u => u.GetActiveTenantSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        List<Project> projects =
        [
            BuildProject(tenantId, "Alpha"),
            BuildProject(tenantId, "Beta"),
        ];

        _projectRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<Project, bool>>>()))
            .ReturnsAsync(projects);

        GetProjectsDictionaryQuery query = ValidQuery(tenantId);

        // Act
        Dictionary<Guid, string> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Values.Should().Contain("Alpha");
        result.Values.Should().Contain("Beta");
    }

    [Fact]
    public async Task Handle_WhenUserIsNotTenantAdmin_ReturnsProjectsFromMemberships()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        Project project = BuildProject(tenantId, "Member Project");
        Role adminRole = new Role { Id = Guid.NewGuid(), Code = RoleCodes.ProjectAdmin };

        List<ProjectMember> memberships =
        [
            new ProjectMember
            {
                ProjectId = project.Id,
                TenantId = tenantId,
                UserId = userId,
                MemberRole = adminRole,
                Project = project,
            },
        ];

        _projectMemberRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<ProjectMember, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectMember>, IIncludableQueryable<ProjectMember, object>>>()))
            .ReturnsAsync(memberships);

        GetProjectsDictionaryQuery query = ValidQuery(tenantId);

        // Act
        Dictionary<Guid, string> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.Values.Should().Contain("Member Project");
    }

    [Fact]
    public async Task Handle_WhenProjectIsInactive_AppendsSuffix()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();

        TenantCtxSnapshot snapshot = new TenantCtxSnapshot(
            TenantId: tenantId,
            TenantRoleId: Guid.NewGuid(),
            TenantPermissionCodes: new HashSet<string>(),
            IsTenantAdmin: true,
            IsActive: true);

        _currentUserMock
            .Setup(u => u.GetActiveTenantSnapshotAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        Project inactiveProject = BuildProject(tenantId, "Old Project", isActive: false);

        _projectRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<Project, bool>>>()))
            .ReturnsAsync(new List<Project> { inactiveProject });

        GetProjectsDictionaryQuery query = ValidQuery(tenantId);

        // Act
        Dictionary<Guid, string> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.Values.First().Should().Contain("[Nieaktywny]");
    }
}
