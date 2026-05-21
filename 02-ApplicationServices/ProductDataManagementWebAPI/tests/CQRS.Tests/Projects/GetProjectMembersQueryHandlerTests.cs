using Business.Interfaces.Constants;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Projects;
using CQRS.Projects.GetProjectMembers;
using FluentAssertions;
using Moq;

namespace CQRS.Tests.Projects;

public sealed class GetProjectMembersQueryHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly GetProjectMembersQueryHandler _handler;

    public GetProjectMembersQueryHandlerTests()
    {
        _handler = new GetProjectMembersQueryHandler(_userServiceMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static GetProjectMembersQuery ValidQuery(Guid tenantId, Guid projectId) =>
        new GetProjectMembersQuery { TenantId = tenantId, ProjectId = projectId };

    private static ProjectMemberUserInfo BuildMember(string firstName, string lastName) =>
        new ProjectMemberUserInfo
        {
            UserId = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName.ToLower()}@example.com",
            AzureAdB2CObjectId = Guid.NewGuid().ToString(),
            RoleCode = RoleCodes.ProjectViewer,
            JoinedAt = DateTime.UtcNow,
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenMembersExist_ReturnsMappedMembers()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        List<ProjectMemberUserInfo> members =
        [
            BuildMember("Alice", "Smith"),
            BuildMember("Bob", "Jones"),
        ];

        _userServiceMock
            .Setup(s => s.GetProjectMembersAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(members);

        GetProjectMembersQuery query = ValidQuery(tenantId, projectId);

        // Act
        IEnumerable<ProjectMemberWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        List<ProjectMemberWeb> list = result.ToList();
        list.Should().HaveCount(2);
        list.Should().AllSatisfy(m => m.RoleCode.Should().Be(RoleCodes.ProjectViewer));
    }

    [Fact]
    public async Task Handle_WhenNoMembers_ReturnsEmptyList()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        _userServiceMock
            .Setup(s => s.GetProjectMembersAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectMemberUserInfo>());

        GetProjectMembersQuery query = ValidQuery(tenantId, projectId);

        // Act
        IEnumerable<ProjectMemberWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MembersAreOrderedByLastNameThenFirstName()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        List<ProjectMemberUserInfo> members =
        [
            BuildMember("Zbigniew", "Nowak"),
            BuildMember("Adam", "Kowalski"),
            BuildMember("Marek", "Kowalski"),
        ];

        _userServiceMock
            .Setup(s => s.GetProjectMembersAsync(tenantId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(members);

        GetProjectMembersQuery query = ValidQuery(tenantId, projectId);

        // Act
        IEnumerable<ProjectMemberWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        List<ProjectMemberWeb> list = result.ToList();
        list[0].LastName.Should().Be("Kowalski");
        list[0].FirstName.Should().Be("Adam");
        list[1].LastName.Should().Be("Kowalski");
        list[1].FirstName.Should().Be("Marek");
        list[2].LastName.Should().Be("Nowak");
    }
}
