using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Projects.UpdateProjectMemberRole;
using Entities.Enums;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Projects;

public sealed class UpdateProjectMemberRoleCommandHandlerTests
{
    private readonly Mock<IReadRepository<Project>> _projectRepoMock = new();
    private readonly Mock<IRepository<ProjectMember>> _projectMemberRepoMock = new();
    private readonly Mock<IReadRepository<Role>> _roleRepoMock = new();
    private readonly Mock<IReadRepository<Notification>> _notificationRepoMock = new();
    private readonly Mock<IPermissionsVersionService> _permissionsVersionServiceMock = new();
    private readonly Mock<INotificationSender> _notificationSenderMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly UpdateProjectMemberRoleCommandHandler _handler;

    public UpdateProjectMemberRoleCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _notificationRepoMock
            .Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<Notification, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _userServiceMock
            .Setup(s => s.GetProjectMemberAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectMemberUserInfo?)null);

        _userServiceMock
            .Setup(s => s.InvalidateProjectMembersCacheAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new UpdateProjectMemberRoleCommandHandler(
            _projectRepoMock.Object,
            _projectMemberRepoMock.Object,
            _roleRepoMock.Object,
            _notificationRepoMock.Object,
            _permissionsVersionServiceMock.Object,
            _notificationSenderMock.Object,
            _currentUserMock.Object,
            _userServiceMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static UpdateProjectMemberRoleCommand ValidCommand() => new UpdateProjectMemberRoleCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        RoleId = Guid.NewGuid(),
    };

    private static Project BuildProject(Guid id, Guid tenantId) => new Project
    {
        Id = id,
        TenantId = tenantId,
        Name = "Test Project",
    };

    private static ProjectMember BuildMember(Guid projectId, Guid tenantId, Guid userId) =>
        new ProjectMember { ProjectId = projectId, TenantId = tenantId, UserId = userId };

    private static Role BuildRole(Guid id) => new Role
    {
        Id = id,
        Code = "PROJECT.EDITOR",
        Name = "Project Editor",
        Scope = RoleScope.Project,
        IsActive = true,
    };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenAllEntitiesExist_UpdatesMemberRoleAndReturnsUnit()
    {
        // Arrange
        UpdateProjectMemberRoleCommand command = ValidCommand();
        Project project = BuildProject(command.ProjectId, command.TenantId);
        ProjectMember member = BuildMember(command.ProjectId, command.TenantId, command.UserId);
        Role newRole = BuildRole(command.RoleId);

        _projectRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<Project, bool>>>()))
            .ReturnsAsync(project);

        _projectMemberRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<ProjectMember, bool>>>()))
            .ReturnsAsync(member);

        _roleRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Role, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(newRole);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        member.RoleId.Should().Be(newRole.Id);
        _projectMemberRepoMock.Verify(r => r.Update(member), Times.Once);
        _permissionsVersionServiceMock.Verify(
            s => s.BumpVersionAsync(command.UserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProjectNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _projectRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<Project, bool>>>()))
            .ReturnsAsync((Project?)null);

        UpdateProjectMemberRoleCommand command = ValidCommand();

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenMemberNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        UpdateProjectMemberRoleCommand command = ValidCommand();
        Project project = BuildProject(command.ProjectId, command.TenantId);

        _projectRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<Project, bool>>>()))
            .ReturnsAsync(project);

        _projectMemberRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<ProjectMember, bool>>>()))
            .ReturnsAsync((ProjectMember?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenNewRoleNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        UpdateProjectMemberRoleCommand command = ValidCommand();
        Project project = BuildProject(command.ProjectId, command.TenantId);
        ProjectMember member = BuildMember(command.ProjectId, command.TenantId, command.UserId);

        _projectRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<Project, bool>>>()))
            .ReturnsAsync(project);

        _projectMemberRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<ProjectMember, bool>>>()))
            .ReturnsAsync(member);

        _roleRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Role, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }
}
