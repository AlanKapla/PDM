using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Projects.AddProjectMember;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using FluentAssertions;
using MediatR;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Projects;

public sealed class AddProjectMemberCommandHandlerTests
{
    private readonly Mock<IReadRepository<Project>> _projectRepoMock = new();
    private readonly Mock<IReadRepository<Notification>> _notificationRepoMock = new();
    private readonly Mock<IProjectMembershipProvisioner> _membershipProvisionerMock = new();
    private readonly Mock<INotificationSender> _notificationSenderMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly AddProjectMemberCommandHandler _handler;

    public AddProjectMemberCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _notificationRepoMock
            .Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<Notification, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _userServiceMock
            .Setup(s => s.GetTenantMemberInfoAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectMemberUserInfo?)null);

        _membershipProvisionerMock
            .Setup(s => s.ProvisionProjectMemberAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                It.IsAny<IReadOnlyList<Entities.Enums.ProjectModule>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new AddProjectMemberCommandHandler(
            _projectRepoMock.Object,
            _notificationRepoMock.Object,
            _notificationSenderMock.Object,
            _currentUserMock.Object,
            _userServiceMock.Object,
            _membershipProvisionerMock.Object);
    }

    private static AddProjectMemberCommand ValidCommand() => new AddProjectMemberCommand
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
        IsActive = true,
    };

    [Fact]
    public async Task Handle_WhenProjectAndRoleExist_InsertsNewMemberAndReturnsUnit()
    {
        // Arrange
        AddProjectMemberCommand command = ValidCommand();
        Project project = BuildProject(command.ProjectId, command.TenantId);

        _projectRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _membershipProvisionerMock.Verify(
            s => s.ProvisionProjectMemberAsync(
                command.TenantId,
                command.ProjectId,
                command.UserId,
                false,
                command.Modules,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProjectNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _projectRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        AddProjectMemberCommand command = ValidCommand();

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }
}
