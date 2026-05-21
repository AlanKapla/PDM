using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Projects;
using CQRS.Projects.CreateProject;
using Entities.Enums;
using Entities.Models.Projects;
using Entities.Models.Roles;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Projects;

public sealed class CreateProjectCommandHandlerTests
{
    private readonly Mock<IReadRepository<Project>> _projectRepoMock = new();
    private readonly Mock<IRepository<ProjectMember>> _projectMemberRepoMock = new();
    private readonly Mock<IReadRepository<Role>> _roleRepoMock = new();
    private readonly Mock<IRepository<ProjectCurrency>> _currencyRepoMock = new();
    private readonly Mock<IPermissionsVersionService> _permissionsVersionServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly CreateProjectCommandHandler _handler;

    public CreateProjectCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());
        _currentUserMock.Setup(u => u.FirstName).Returns("Jan");
        _currentUserMock.Setup(u => u.LastName).Returns("Kowalski");
        _currentUserMock
            .Setup(u => u.GetProjectSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectCtxSnapshot?)null);

        _handler = new CreateProjectCommandHandler(
            _projectRepoMock.Object,
            _projectMemberRepoMock.Object,
            _roleRepoMock.Object,
            _currencyRepoMock.Object,
            _permissionsVersionServiceMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static CreateProjectCommand ValidCommand() => new CreateProjectCommand
    {
        TenantId = Guid.NewGuid(),
        Name = "Test Project",
    };

    private static Role BuildAdminRole() => new Role
    {
        Id = Guid.NewGuid(),
        Code = RoleCodes.ProjectAdmin,
        Scope = RoleScope.Project,
        IsActive = true,
    };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenCommandIsValid_CreatesProjectAndReturnsDetails()
    {
        // Arrange
        Role adminRole = BuildAdminRole();

        _roleRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Role, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminRole);

        CreateProjectCommand command = ValidCommand();

        // Act
        ProjectDetailsWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TenantId.Should().Be(command.TenantId);
        result.Name.Should().Be(command.Name);
        result.IsActive.Should().BeTrue();
        result.MembersCount.Should().Be(1);
        result.UserRoleCode.Should().Be(RoleCodes.ProjectAdmin);
        result.Currency.Should().NotBeNull();
        result.Currency!.Code.Should().Be("PLN");

        _projectRepoMock.Verify(r => r.Insert(It.IsAny<Project>()), Times.Once);
        _projectMemberRepoMock.Verify(r => r.Insert(It.IsAny<ProjectMember>()), Times.Once);
        _currencyRepoMock.Verify(r => r.Insert(It.IsAny<ProjectCurrency>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAdminRoleNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _roleRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Role, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        CreateProjectCommand command = ValidCommand();

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenProjectCreated_BumpsPermissionsVersionForCurrentUser()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        _roleRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Role, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildAdminRole());

        CreateProjectCommand command = ValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _permissionsVersionServiceMock.Verify(
            s => s.BumpVersionAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
