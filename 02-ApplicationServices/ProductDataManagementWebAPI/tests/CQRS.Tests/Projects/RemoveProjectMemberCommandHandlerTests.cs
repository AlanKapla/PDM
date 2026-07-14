using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Projects.RemoveProjectMember;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Projects;

public sealed class RemoveProjectMemberCommandHandlerTests
{
    private readonly Mock<IReadRepository<Project>> _projectRepoMock = new();
    private readonly Mock<IRepository<ProjectMember>> _projectMemberRepoMock = new();
    private readonly Mock<IReadRepository<Notification>> _notificationRepoMock = new();
    private readonly Mock<INotificationSender> _notificationSenderMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<IPermissionsVersionService> _permissionsVersionServiceMock = new();
    private readonly RemoveProjectMemberCommandHandler _handler;

    public RemoveProjectMemberCommandHandlerTests()
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

        _permissionsVersionServiceMock
            .Setup(s => s.BumpVersionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new RemoveProjectMemberCommandHandler(
            _projectRepoMock.Object,
            _projectMemberRepoMock.Object,
            _notificationSenderMock.Object,
            _currentUserMock.Object,
            _notificationRepoMock.Object,
            _userServiceMock.Object,
            _permissionsVersionServiceMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static RemoveProjectMemberCommand ValidCommand() => new RemoveProjectMemberCommand
    {
        TenantId = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
    };

    private static Project BuildProject(Guid id, Guid tenantId) => new Project
    {
        Id = id,
        TenantId = tenantId,
        Name = "Test Project",
    };

    private static ProjectMember BuildMember(Guid projectId, Guid tenantId, Guid userId) =>
        new ProjectMember { ProjectId = projectId, TenantId = tenantId, UserId = userId };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenProjectAndMemberExist_DeactivatesMemberAndReturnsUnit()
    {
        // Arrange
        RemoveProjectMemberCommand command = ValidCommand();
        Project project = BuildProject(command.ProjectId, command.TenantId);
        ProjectMember member = BuildMember(command.ProjectId, command.TenantId, command.UserId);
        member.IsActive = true;

        _projectRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<Project, bool>>>()))
            .ReturnsAsync(project);

        _projectMemberRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<ProjectMember, bool>>>()))
            .ReturnsAsync(member);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        member.IsActive.Should().BeFalse();
        _projectMemberRepoMock.Verify(r => r.Update(member), Times.Once);
        _userServiceMock.Verify(
            s => s.InvalidateProjectMembersCacheAsync(command.TenantId, command.ProjectId, It.IsAny<CancellationToken>()),
            Times.Once);
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

        RemoveProjectMemberCommand command = ValidCommand();

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenMemberNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        RemoveProjectMemberCommand command = ValidCommand();
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
}
