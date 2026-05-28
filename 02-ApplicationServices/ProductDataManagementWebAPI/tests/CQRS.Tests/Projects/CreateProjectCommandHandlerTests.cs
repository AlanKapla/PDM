using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Projects;
using CQRS.Projects.CreateProject;
using Entities.Models.Projects;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Projects;

public sealed class CreateProjectCommandHandlerTests
{
    private readonly Mock<IReadRepository<Project>> _projectRepoMock = new();
    private readonly Mock<IRepository<ProjectMember>> _projectMemberRepoMock = new();
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

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenCommandIsValid_CreatesProjectAndReturnsDetails()
    {
        // Arrange
        CreateProjectCommand command = ValidCommand();

        // Act
        ProjectDetailsWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TenantId.Should().Be(command.TenantId);
        result.Name.Should().Be(command.Name);
        result.IsActive.Should().BeTrue();
        result.MembersCount.Should().Be(1);
        result.IsAdmin.Should().BeTrue();
        result.Currency.Should().NotBeNull();
        result.Currency!.Code.Should().Be("PLN");

        _projectRepoMock.Verify(r => r.Insert(It.IsAny<Project>()), Times.Once);
        _projectMemberRepoMock.Verify(r => r.Insert(It.IsAny<ProjectMember>()), Times.Once);
        _currencyRepoMock.Verify(r => r.Insert(It.IsAny<ProjectCurrency>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProjectCreated_BumpsPermissionsVersionForCurrentUser()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        CreateProjectCommand command = ValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _permissionsVersionServiceMock.Verify(
            s => s.BumpVersionAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
