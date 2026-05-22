using Business.Implementation.Services;
using Business.Interfaces.Services;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repositories.Repository.Interfaces;

namespace Business.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly Mock<IRepository<ProjectMember>> _projectMemberRepoMock = new();
    private readonly Mock<IRepository<TenantMember>> _tenantMemberRepoMock = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(
            _cacheMock.Object,
            _projectMemberRepoMock.Object,
            _tenantMemberRepoMock.Object,
            NullLogger<UserService>.Instance);
    }

    // ─── GetProjectMembersAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetProjectMembersAsync_WhenCacheHit_ReturnsCachedMembers()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        List<ProjectMemberUserInfo> cached =
        [
            new() { UserId = Guid.NewGuid(), FirstName = "Jan", LastName = "Kowalski", Email = "jan@test.com" }
        ];

        _cacheMock
            .Setup(c => c.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<ProjectMemberUserInfo>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        // Act
        List<ProjectMemberUserInfo> result = await _sut.GetProjectMembersAsync(tenantId, projectId);

        // Assert
        result.Should().BeEquivalentTo(cached);
        _projectMemberRepoMock.Verify(r => r.GetBySearch(
            It.IsAny<System.Linq.Expressions.Expression<Func<ProjectMember, bool>>>(),
            It.IsAny<Func<IQueryable<ProjectMember>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<ProjectMember, object>>[]>()),
            Times.Never);
    }

    [Fact]
    public async Task GetProjectMembersAsync_WhenCacheReturnsNull_ReturnsEmptyList()
    {
        // Arrange
        _cacheMock
            .Setup(c => c.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<ProjectMemberUserInfo>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<ProjectMemberUserInfo>?)null);

        // Act
        List<ProjectMemberUserInfo> result = await _sut.GetProjectMembersAsync(
            Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProjectMembersAsync_CacheKeyContainsTenantAndProject()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        string capturedKey = string.Empty;

        _cacheMock
            .Setup(c => c.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<ProjectMemberUserInfo>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Func<Task<List<ProjectMemberUserInfo>>>, TimeSpan?, CancellationToken>(
                (key, _, _, _) => capturedKey = key)
            .ReturnsAsync([]);

        // Act
        await _sut.GetProjectMembersAsync(tenantId, projectId);

        // Assert
        capturedKey.Should().Contain(tenantId.ToString());
        capturedKey.Should().Contain(projectId.ToString());
    }

    // ─── GetProjectMemberAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetProjectMemberAsync_UserExists_ReturnsMember()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        ProjectMemberUserInfo info = new() { UserId = userId, Email = "user@test.com" };

        _cacheMock
            .Setup(c => c.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<ProjectMemberUserInfo>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([info]);

        // Act
        ProjectMemberUserInfo? result = await _sut.GetProjectMemberAsync(tenantId, projectId, userId);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetProjectMemberAsync_UserNotInList_ReturnsNull()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid otherUserId = Guid.NewGuid();

        _cacheMock
            .Setup(c => c.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<ProjectMemberUserInfo>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ProjectMemberUserInfo { UserId = Guid.NewGuid() }]);

        // Act
        ProjectMemberUserInfo? result = await _sut.GetProjectMemberAsync(tenantId, projectId, otherUserId);

        // Assert
        result.Should().BeNull();
    }

    // ─── InvalidateProjectMembersCacheAsync ──────────────────────────────────

    [Fact]
    public async Task InvalidateProjectMembersCacheAsync_CallsRemoveWithCorrectKey()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        string expectedKey = $"users:{tenantId}:{projectId}:members";
        string capturedKey = string.Empty;

        _cacheMock
            .Setup(c => c.RemoveCacheByKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((key, _) => capturedKey = key)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.InvalidateProjectMembersCacheAsync(tenantId, projectId);

        // Assert
        capturedKey.Should().Be(expectedKey);
    }

    // ─── GetTenantMemberInfoAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetTenantMemberInfoAsync_MemberFound_ReturnsMappedInfo()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        TenantMember tenantMember = new()
        {
            TenantId = tenantId,
            UserId = userId,
            User = new User
            {
                Id = userId,
                FirstName = "Anna",
                LastName = "Nowak",
                Email = "anna@test.com",
                AzureAdB2CObjectId = "obj-123"
            }
        };

        _tenantMemberRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<TenantMember, bool>>>(),
                It.IsAny<Func<IQueryable<TenantMember>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<TenantMember, object>>[]>()))
            .ReturnsAsync(tenantMember);

        // Act
        ProjectMemberUserInfo? result = await _sut.GetTenantMemberInfoAsync(tenantId, userId);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.FirstName.Should().Be("Anna");
        result.LastName.Should().Be("Nowak");
        result.Email.Should().Be("anna@test.com");
        result.AzureAdB2CObjectId.Should().Be("obj-123");
    }

    [Fact]
    public async Task GetTenantMemberInfoAsync_MemberNotFound_ReturnsNull()
    {
        // Arrange
        _tenantMemberRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<System.Linq.Expressions.Expression<Func<TenantMember, bool>>>(),
                It.IsAny<Func<IQueryable<TenantMember>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<TenantMember, object>>[]>()))
            .ReturnsAsync((TenantMember?)null);

        // Act
        ProjectMemberUserInfo? result = await _sut.GetTenantMemberInfoAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    // ─── GetProjectMembersByIdsAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetProjectMembersByIdsAsync_EmptyIds_ReturnsEmptyDictionary()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        // Act
        Dictionary<Guid, ProjectMemberUserInfo> result = await _sut.GetProjectMembersByIdsAsync(
            tenantId, projectId, new HashSet<Guid>());

        // Assert
        result.Should().BeEmpty();
        _cacheMock.Verify(c => c.GetOrAddAsync(
            It.IsAny<string>(),
            It.IsAny<Func<Task<List<ProjectMemberUserInfo>>>>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetProjectMembersByIdsAsync_FiltersToRequestedIds()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid requestedId = Guid.NewGuid();
        Guid otherId = Guid.NewGuid();

        ProjectMemberUserInfo requested = new() { UserId = requestedId };
        ProjectMemberUserInfo other = new() { UserId = otherId };

        _cacheMock
            .Setup(c => c.GetOrAddAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<List<ProjectMemberUserInfo>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([requested, other]);

        // Act
        Dictionary<Guid, ProjectMemberUserInfo> result = await _sut.GetProjectMembersByIdsAsync(
            tenantId, projectId, new HashSet<Guid> { requestedId });

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainKey(requestedId);
        result.Should().NotContainKey(otherId);
    }
}
