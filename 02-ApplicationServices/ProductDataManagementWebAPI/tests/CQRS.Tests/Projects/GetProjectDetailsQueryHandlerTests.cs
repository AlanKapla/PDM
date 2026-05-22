using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;
using CQRS.Projects.GetProjectDetails;
using Entities.Models.Projects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Projects;

public sealed class GetProjectDetailsQueryHandlerTests
{
    private readonly Mock<IReadRepository<Project>> _projectRepoMock = new();
    private readonly Mock<IRepository<ProjectMember>> _projectMemberRepoMock = new();
    private readonly Mock<IReadRepository<ProjectCurrency>> _currencyRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetProjectDetailsQueryHandler _handler;

    public GetProjectDetailsQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(false);
        _currentUserMock
            .Setup(u => u.GetTenantSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantCtxSnapshot?)null);
        _currentUserMock
            .Setup(u => u.GetProjectSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectCtxSnapshot?)null);

        _projectMemberRepoMock
            .Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<ProjectMember, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        _projectMemberRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ProjectMember, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectMember>, IIncludableQueryable<ProjectMember, object>>>()))
            .ReturnsAsync((ProjectMember?)null);

        _currencyRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ProjectCurrency, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectCurrency?)null);

        _handler = new GetProjectDetailsQueryHandler(
            _projectRepoMock.Object,
            _projectMemberRepoMock.Object,
            _currencyRepoMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static GetProjectDetailsQuery ValidQuery(Guid tenantId, Guid projectId) =>
        new GetProjectDetailsQuery { TenantId = tenantId, ProjectId = projectId };

    private static Project BuildProject(Guid id, Guid tenantId) => new Project
    {
        Id = id,
        TenantId = tenantId,
        Name = "My Project",
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        CreatedByUserId = Guid.NewGuid(),
    };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenProjectExists_ReturnsProjectDetails()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Project project = BuildProject(projectId, tenantId);

        _projectRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<Project>, IIncludableQueryable<Project, object>>>()))
            .ReturnsAsync(project);

        GetProjectDetailsQuery query = ValidQuery(tenantId, projectId);

        // Act
        ProjectDetailsWeb result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(projectId);
        result.TenantId.Should().Be(tenantId);
        result.Name.Should().Be(project.Name);
        result.IsActive.Should().BeTrue();
        result.MembersCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenProjectNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _projectRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<Project>, IIncludableQueryable<Project, object>>>()))
            .ReturnsAsync((Project?)null);

        GetProjectDetailsQuery query = ValidQuery(Guid.NewGuid(), Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenUserHasProjectMembership_ReturnsProjectMemberRoleCode()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Project project = BuildProject(projectId, tenantId);

        _projectRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Func<IQueryable<Project>, IIncludableQueryable<Project, object>>>()))
            .ReturnsAsync(project);

        ProjectMember membership = new ProjectMember
        {
            ProjectId = projectId,
            TenantId = tenantId,
            UserId = _currentUserMock.Object.Id,
            MemberRole = new Entities.Models.Roles.Role { Code = RoleCodes.ProjectEditor },
        };

        _projectMemberRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<ProjectMember, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectMember>, IIncludableQueryable<ProjectMember, object>>>()))
            .ReturnsAsync(membership);

        GetProjectDetailsQuery query = ValidQuery(tenantId, projectId);

        // Act
        ProjectDetailsWeb result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.UserRoleCode.Should().Be(RoleCodes.ProjectEditor);
    }
}
