using Business.Implementation.Services.Files;
using Business.Interfaces.Services;
using FluentAssertions;

namespace Business.Tests.Services.Files;

public class ProjectMemberNameResolverTests
{
    // ─── ResolveUserName ──────────────────────────────────────────────────────

    [Fact]
    public void ResolveUserName_UserFoundInDict_ReturnsFullName()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        ProjectMemberUserInfo userInfo = new ProjectMemberUserInfo
        {
            UserId = userId,
            FirstName = "Anna",
            LastName = "Kowalska"
        };
        Dictionary<Guid, ProjectMemberUserInfo> userDict = new Dictionary<Guid, ProjectMemberUserInfo>
        {
            [userId] = userInfo
        };

        // Act
        string result = ProjectMemberNameResolver.ResolveUserName(userDict, userId);

        // Assert
        result.Should().Be("Anna Kowalska");
    }

    [Fact]
    public void ResolveUserName_UserNotFoundInDict_ReturnsEmptyString()
    {
        // Arrange
        Guid unknownUserId = Guid.NewGuid();
        Dictionary<Guid, ProjectMemberUserInfo> userDict = new Dictionary<Guid, ProjectMemberUserInfo>();

        // Act
        string result = ProjectMemberNameResolver.ResolveUserName(userDict, unknownUserId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ResolveUserName_UserHasOnlyFirstName_ReturnsTrimmedFullName()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        ProjectMemberUserInfo userInfo = new ProjectMemberUserInfo
        {
            UserId = userId,
            FirstName = "Jan",
            LastName = string.Empty
        };
        Dictionary<Guid, ProjectMemberUserInfo> userDict = new Dictionary<Guid, ProjectMemberUserInfo>
        {
            [userId] = userInfo
        };

        // Act
        string result = ProjectMemberNameResolver.ResolveUserName(userDict, userId);

        // Assert
        result.Should().Be("Jan");
    }
}
