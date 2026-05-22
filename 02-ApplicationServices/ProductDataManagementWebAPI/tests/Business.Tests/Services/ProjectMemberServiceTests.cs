using Business.Implementation.Services;
using Business.Interfaces.DTO;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using FluentAssertions;
using Moq;
using Repositories.Repository.Interfaces;

namespace Business.Tests.Services;

public class ProjectMemberServiceTests
{
    private readonly Mock<IRepository<ProjectMember>> _memberRepoMock = new();
    private readonly Mock<IReadRepository<User>> _userRepoMock = new();
    private readonly Mock<IReadRepository<Project>> _projectRepoMock = new();
    private readonly Mock<IReadRepository<Tenant>> _tenantRepoMock = new();
    private readonly ProjectMemberService _sut;

    public ProjectMemberServiceTests()
    {
        _sut = new ProjectMemberService(
            _memberRepoMock.Object,
            _userRepoMock.Object,
            _projectRepoMock.Object,
            _tenantRepoMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static User BuildUser(Guid userId, string firstName = "Jan", string lastName = "Kowalski")
        => new User
        {
            Id = userId,
            Email = "test@test.com",
            FirstName = firstName,
            LastName = lastName,
            AzureAdB2CObjectId = Guid.NewGuid().ToString()
        };

    private static ProjectMember BuildMember(Guid projectId, Guid userId)
        => new ProjectMember { ProjectId = projectId, UserId = userId };

    // ─── FindSharedProjectAsync ───────────────────────────────────────────────

    [Fact]
    public async Task FindSharedProjectAsync_User1HasNoProjects_ReturnsNull()
    {
        // Arrange
        Guid user1 = Guid.NewGuid();
        Guid user2 = Guid.NewGuid();

        _memberRepoMock
            .Setup(r => r.SelectAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectMember, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectMember, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        // Act
        ProjectMember? result = await _sut.FindSharedProjectAsync(user1, user2, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindSharedProjectAsync_CommonProjectFound_ReturnsMember()
    {
        // Arrange
        Guid user1 = Guid.NewGuid();
        Guid user2 = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        ProjectMember expectedMember = BuildMember(projectId, user2);

        _memberRepoMock
            .Setup(r => r.SelectAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectMember, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectMember, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { projectId });

        _memberRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectMember, bool>>>()))
            .ReturnsAsync(expectedMember);

        // Act
        ProjectMember? result = await _sut.FindSharedProjectAsync(user1, user2, CancellationToken.None);

        // Assert
        result.Should().Be(expectedMember);
    }

    // ─── FindCommonProjectForAllAsync ─────────────────────────────────────────

    [Fact]
    public async Task FindCommonProjectForAllAsync_EmptyUserIds_ReturnsNull()
    {
        // Arrange
        IEnumerable<Guid> emptyIds = Enumerable.Empty<Guid>();

        // Act
        ProjectMember? result = await _sut.FindCommonProjectForAllAsync(emptyIds, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindCommonProjectForAllAsync_NoCommonProject_ReturnsNull()
    {
        // Arrange
        Guid user1 = Guid.NewGuid();
        Guid user2 = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        _memberRepoMock
            .Setup(r => r.SelectAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectMember, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectMember, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { projectId });

        _memberRepoMock
            .Setup(r => r.CountAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectMember, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // Only 1 member found — not all 2 users are in the project

        // Act
        ProjectMember? result = await _sut.FindCommonProjectForAllAsync(
            new[] { user1, user2 }, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindCommonProjectForAllAsync_CommonProjectExists_ReturnsMember()
    {
        // Arrange
        Guid user1 = Guid.NewGuid();
        Guid user2 = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        ProjectMember expectedMember = BuildMember(projectId, user1);

        _memberRepoMock
            .Setup(r => r.SelectAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectMember, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectMember, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { projectId });

        _memberRepoMock
            .Setup(r => r.CountAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectMember, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2); // Both users are in the project

        _memberRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectMember, bool>>>()))
            .ReturnsAsync(expectedMember);

        // Act
        ProjectMember? result = await _sut.FindCommonProjectForAllAsync(
            new[] { user1, user2 }, CancellationToken.None);

        // Assert
        result.Should().Be(expectedMember);
    }

    // ─── IsUserInProjectAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task IsUserInProjectAsync_UserIsMember_ReturnsTrue()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        _memberRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectMember, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        bool result = await _sut.IsUserInProjectAsync(userId, projectId, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsUserInProjectAsync_UserIsNotMember_ReturnsFalse()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        _memberRepoMock
            .Setup(r => r.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProjectMember, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        bool result = await _sut.IsUserInProjectAsync(userId, projectId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    // ─── GetUserDisplayNameAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetUserDisplayNameAsync_UserFound_ReturnsFullName()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        User user = BuildUser(userId, "Anna", "Nowak");

        _userRepoMock
            .Setup(r => r.GetById(userId))
            .ReturnsAsync(user);

        // Act
        string result = await _sut.GetUserDisplayNameAsync(userId, CancellationToken.None);

        // Assert
        result.Should().Be("Anna Nowak");
    }

    [Fact]
    public async Task GetUserDisplayNameAsync_UserNotFound_ReturnsUserIdString()
    {
        // Arrange
        Guid userId = Guid.NewGuid();

        _userRepoMock
            .Setup(r => r.GetById(userId))
            .ReturnsAsync((User?)null);

        // Act
        string result = await _sut.GetUserDisplayNameAsync(userId, CancellationToken.None);

        // Assert
        result.Should().Be(userId.ToString());
    }

    // ─── GetUserNamesByIdsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetUserNamesByIdsAsync_EmptyIds_ReturnsEmptyDictionary()
    {
        // Arrange
        IEnumerable<Guid> empty = Enumerable.Empty<Guid>();

        // Act
        Dictionary<Guid, (string FirstName, string LastName)> result =
            await _sut.GetUserNamesByIdsAsync(empty, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        _userRepoMock.Verify(
            r => r.GetBySearch(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()),
            Times.Never);
    }

    [Fact]
    public async Task GetUserNamesByIdsAsync_IdsProvided_ReturnsDictionary()
    {
        // Arrange
        Guid id1 = Guid.NewGuid();
        Guid id2 = Guid.NewGuid();
        List<User> users = new List<User>
        {
            BuildUser(id1, "Jan", "Kowalski"),
            BuildUser(id2, "Anna", "Nowak")
        };

        _userRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(users);

        // Act
        Dictionary<Guid, (string FirstName, string LastName)> result =
            await _sut.GetUserNamesByIdsAsync(new[] { id1, id2 }, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[id1].Should().Be(("Jan", "Kowalski"));
        result[id2].Should().Be(("Anna", "Nowak"));
    }
}
