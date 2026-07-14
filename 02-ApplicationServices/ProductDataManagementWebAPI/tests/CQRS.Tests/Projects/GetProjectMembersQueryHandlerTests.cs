using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Projects;
using CQRS.Projects.GetProjectMembers;
using Entities.Models.Projects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Projects;

public sealed class GetProjectMembersQueryHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<IRepository<ProjectMember>> _projectMemberRepoMock = new();
    private readonly GetProjectMembersQueryHandler _handler;

    public GetProjectMembersQueryHandlerTests()
    {
        _projectMemberRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<ProjectMember, bool>>>(),
                It.IsAny<Func<IQueryable<ProjectMember>, IIncludableQueryable<ProjectMember, object>>[]>()))
            .ReturnsAsync(new List<ProjectMember>());

        _handler = new GetProjectMembersQueryHandler(_userServiceMock.Object, _projectMemberRepoMock.Object);
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
        list.Should().AllSatisfy(m => m.Email.Should().NotBeNullOrEmpty());
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
