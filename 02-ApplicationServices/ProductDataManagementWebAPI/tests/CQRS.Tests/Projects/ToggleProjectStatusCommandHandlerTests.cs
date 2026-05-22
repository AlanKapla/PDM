using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Projects.ToggleProjectStatus;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Users;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Projects;

public sealed class ToggleProjectStatusCommandHandlerTests
{
    private readonly Mock<IRepository<Project>> _projectRepoMock = new();
    private readonly Mock<IReadRepository<User>> _userRepoMock = new();
    private readonly Mock<IRepository<ProjectMember>> _projectMemberRepoMock = new();
    private readonly Mock<IReadRepository<Notification>> _notificationRepoMock = new();
    private readonly Mock<INotificationSender> _notificationSenderMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly ToggleProjectStatusCommandHandler _handler;

    public ToggleProjectStatusCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _projectMemberRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<ProjectMember, bool>>>()))
            .ReturnsAsync(new List<ProjectMember>());

        _userRepoMock
            .Setup(r => r.GetBySearch(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User>());

        _notificationRepoMock
            .Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<Notification, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _handler = new ToggleProjectStatusCommandHandler(
            _projectRepoMock.Object,
            _userRepoMock.Object,
            _projectMemberRepoMock.Object,
            _notificationRepoMock.Object,
            _notificationSenderMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ToggleProjectStatusCommand ValidCommand(Guid tenantId, Guid projectId, bool isActive) =>
        new ToggleProjectStatusCommand
        {
            TenantId = tenantId,
            ProjectId = projectId,
            IsActive = isActive,
        };

    private static Project BuildProject(Guid id, Guid tenantId, bool isActive) => new Project
    {
        Id = id,
        TenantId = tenantId,
        Name = "My Project",
        IsActive = isActive,
    };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenProjectExists_ActivatesProject()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Project project = BuildProject(projectId, tenantId, isActive: false);

        _projectRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<Project, bool>>>()))
            .ReturnsAsync(project);

        ToggleProjectStatusCommand command = ValidCommand(tenantId, projectId, isActive: true);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        project.IsActive.Should().BeTrue();
        _projectRepoMock.Verify(r => r.Update(project), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProjectNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _projectRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<Project, bool>>>()))
            .ReturnsAsync((Project?)null);

        ToggleProjectStatusCommand command = ValidCommand(Guid.NewGuid(), Guid.NewGuid(), isActive: false);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenDeactivating_SetsIsActiveFalse()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Project project = BuildProject(projectId, tenantId, isActive: true);

        _projectRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<Project, bool>>>()))
            .ReturnsAsync(project);

        ToggleProjectStatusCommand command = ValidCommand(tenantId, projectId, isActive: false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        project.IsActive.Should().BeFalse();
    }
}
