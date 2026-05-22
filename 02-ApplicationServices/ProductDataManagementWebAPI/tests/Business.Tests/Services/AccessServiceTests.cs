using Business.Implementation.Services;
using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Business.Tests.Services;

public class AccessServiceTests
{
    private readonly Mock<ILogger<AccessService>> _loggerMock = new Mock<ILogger<AccessService>>();
    private readonly AccessService _sut;

    public AccessServiceTests()
    {
        _sut = new AccessService(_loggerMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static Mock<ICurrentUser> AuthenticatedUser(Guid? activeTenantId = null, bool isSuperAdmin = false)
    {
        Mock<ICurrentUser> mock = new Mock<ICurrentUser>();
        mock.Setup(u => u.IsAuthenticated).Returns(true);
        mock.Setup(u => u.IsSuperAdmin).Returns(isSuperAdmin);
        mock.Setup(u => u.ActiveTenantId).Returns(activeTenantId);
        mock.Setup(u => u.Id).Returns(Guid.NewGuid());
        return mock;
    }

    private static Mock<ICurrentUser> UnauthenticatedUser()
    {
        Mock<ICurrentUser> mock = new Mock<ICurrentUser>();
        mock.Setup(u => u.IsAuthenticated).Returns(false);
        return mock;
    }

    // ─── Unauthenticated ──────────────────────────────────────────────────────

    [Fact]
    public async Task AuthorizeAsync_UnauthenticatedUser_ReturnsFalse()
    {
        // Arrange
        Mock<ICurrentUser> user = UnauthenticatedUser();
        ResourceRef resource = new ResourceRef(TenantId: Guid.NewGuid());

        // Act
        bool result = await _sut.AuthorizeAsync(user.Object, PermissionCodes.ProjectView, resource);

        // Assert
        result.Should().BeFalse();
    }

    // ─── Global scope ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(PermissionCodes.TenantListAvailable)]
    [InlineData(PermissionCodes.TenantAdminListAvailable)]
    [InlineData(PermissionCodes.RoleList)]
    public async Task AuthorizeAsync_GlobalScopePermission_AuthenticatedUser_ReturnsTrue(string permissionCode)
    {
        // Arrange
        Mock<ICurrentUser> user = AuthenticatedUser();
        ResourceRef resource = new ResourceRef(TenantId: Guid.Empty);

        // Act
        bool result = await _sut.AuthorizeAsync(user.Object, permissionCode, resource);

        // Assert
        result.Should().BeTrue();
    }

    // ─── Tenant scope ─────────────────────────────────────────────────────────

    [Fact]
    public async Task AuthorizeAsync_TenantScope_EmptyTenantId_ReturnsFalse()
    {
        // Arrange
        Mock<ICurrentUser> user = AuthenticatedUser(activeTenantId: Guid.NewGuid());
        ResourceRef resource = new ResourceRef(TenantId: Guid.Empty);

        // Act
        bool result = await _sut.AuthorizeAsync(user.Object, PermissionCodes.TenantView, resource);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AuthorizeAsync_TenantScope_ActiveTenantMismatch_ReturnsFalse()
    {
        // Arrange
        Guid activeTenantId = Guid.NewGuid();
        Guid otherTenantId = Guid.NewGuid();
        Mock<ICurrentUser> user = AuthenticatedUser(activeTenantId: activeTenantId);
        ResourceRef resource = new ResourceRef(TenantId: otherTenantId);

        // Act
        bool result = await _sut.AuthorizeAsync(user.Object, PermissionCodes.TenantView, resource);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AuthorizeAsync_TenantScope_MatchingTenant_NoSnapshot_ReturnsFalse()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Mock<ICurrentUser> user = AuthenticatedUser(activeTenantId: tenantId);
        user.Setup(u => u.GetTenantSnapshotAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantCtxSnapshot?)null);
        ResourceRef resource = new ResourceRef(TenantId: tenantId);

        // Act
        bool result = await _sut.AuthorizeAsync(user.Object, PermissionCodes.TenantView, resource);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AuthorizeAsync_TenantScope_MatchingTenant_HasPermission_ActiveTenant_ReturnsTrue()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Mock<ICurrentUser> user = AuthenticatedUser(activeTenantId: tenantId);
        TenantCtxSnapshot snapshot = new TenantCtxSnapshot(
            TenantId: tenantId,
            TenantRoleId: Guid.NewGuid(),
            TenantPermissionCodes: new HashSet<string> { PermissionCodes.TenantView },
            IsTenantAdmin: false,
            IsActive: true);
        user.Setup(u => u.GetTenantSnapshotAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);
        ResourceRef resource = new ResourceRef(TenantId: tenantId);

        // Act
        bool result = await _sut.AuthorizeAsync(user.Object, PermissionCodes.TenantView, resource);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizeAsync_TenantScope_HasPermission_InactiveTenant_NonAdmin_ReturnsFalse()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Mock<ICurrentUser> user = AuthenticatedUser(activeTenantId: tenantId, isSuperAdmin: false);
        TenantCtxSnapshot snapshot = new TenantCtxSnapshot(
            TenantId: tenantId,
            TenantRoleId: Guid.NewGuid(),
            TenantPermissionCodes: new HashSet<string> { PermissionCodes.TenantView },
            IsTenantAdmin: false,
            IsActive: false);
        user.Setup(u => u.GetTenantSnapshotAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);
        ResourceRef resource = new ResourceRef(TenantId: tenantId);

        // Act
        bool result = await _sut.AuthorizeAsync(user.Object, PermissionCodes.TenantView, resource);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AuthorizeAsync_TenantScope_HasPermission_InactiveTenant_TenantAdmin_ReturnsTrue()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Mock<ICurrentUser> user = AuthenticatedUser(activeTenantId: tenantId, isSuperAdmin: false);
        TenantCtxSnapshot snapshot = new TenantCtxSnapshot(
            TenantId: tenantId,
            TenantRoleId: Guid.NewGuid(),
            TenantPermissionCodes: new HashSet<string> { PermissionCodes.TenantView },
            IsTenantAdmin: true,
            IsActive: false);
        user.Setup(u => u.GetTenantSnapshotAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);
        ResourceRef resource = new ResourceRef(TenantId: tenantId);

        // Act
        bool result = await _sut.AuthorizeAsync(user.Object, PermissionCodes.TenantView, resource);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizeAsync_TenantScope_MissingPermission_ReturnsFalse()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Mock<ICurrentUser> user = AuthenticatedUser(activeTenantId: tenantId);
        TenantCtxSnapshot snapshot = new TenantCtxSnapshot(
            TenantId: tenantId,
            TenantRoleId: Guid.NewGuid(),
            TenantPermissionCodes: new HashSet<string>(), // no permissions
            IsTenantAdmin: false,
            IsActive: true);
        user.Setup(u => u.GetTenantSnapshotAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);
        ResourceRef resource = new ResourceRef(TenantId: tenantId);

        // Act
        bool result = await _sut.AuthorizeAsync(user.Object, PermissionCodes.TenantView, resource);

        // Assert
        result.Should().BeFalse();
    }

    // ─── Project scope ────────────────────────────────────────────────────────

    [Fact]
    public async Task AuthorizeAsync_ProjectScope_NoProjectId_ReturnsFalse()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Mock<ICurrentUser> user = AuthenticatedUser(activeTenantId: tenantId);
        ResourceRef resource = new ResourceRef(TenantId: tenantId, ProjectId: null);

        // Act
        bool result = await _sut.AuthorizeAsync(user.Object, PermissionCodes.ProjectView, resource);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AuthorizeAsync_ProjectScope_NoProjectSnapshot_ReturnsFalse()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Mock<ICurrentUser> user = AuthenticatedUser(activeTenantId: tenantId);
        user.Setup(u => u.GetProjectSnapshotAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectCtxSnapshot?)null);
        ResourceRef resource = new ResourceRef(TenantId: tenantId, ProjectId: projectId);

        // Act
        bool result = await _sut.AuthorizeAsync(user.Object, PermissionCodes.ProjectView, resource);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AuthorizeAsync_ProjectScope_HasPermission_ReturnsTrue()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Mock<ICurrentUser> user = AuthenticatedUser(activeTenantId: tenantId);
        ProjectCtxSnapshot snapshot = new ProjectCtxSnapshot(
            ProjectId: projectId,
            TenantId: tenantId,
            ProjectRoleId: Guid.NewGuid(),
            ProjectPermissionCodes: new HashSet<string> { PermissionCodes.ProjectView },
            IsProjectAdmin: false,
            IsActive: true);
        user.Setup(u => u.GetProjectSnapshotAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);
        ResourceRef resource = new ResourceRef(TenantId: tenantId, ProjectId: projectId);

        // Act
        bool result = await _sut.AuthorizeAsync(user.Object, PermissionCodes.ProjectView, resource);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizeAsync_ProjectScope_MissingPermission_ReturnsFalse()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Mock<ICurrentUser> user = AuthenticatedUser(activeTenantId: tenantId);
        ProjectCtxSnapshot snapshot = new ProjectCtxSnapshot(
            ProjectId: projectId,
            TenantId: tenantId,
            ProjectRoleId: Guid.NewGuid(),
            ProjectPermissionCodes: new HashSet<string>(), // no permissions
            IsProjectAdmin: false,
            IsActive: true);
        user.Setup(u => u.GetProjectSnapshotAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);
        ResourceRef resource = new ResourceRef(TenantId: tenantId, ProjectId: projectId);

        // Act
        bool result = await _sut.AuthorizeAsync(user.Object, PermissionCodes.ProjectView, resource);

        // Assert
        result.Should().BeFalse();
    }

    // ─── Cross-tenant scope ────────────────────────────────────────────────────

    [Theory]
    [InlineData(PermissionCodes.TenantEdit)]
    [InlineData(PermissionCodes.TenantMembersManage)]
    [InlineData(PermissionCodes.TenantStatusManage)]
    public async Task AuthorizeAsync_CrossTenantPermission_DifferentTenant_ReachesSnapshotCheck(string permissionCode)
    {
        // Arrange — user's active tenant differs from resource tenant
        Guid activeTenantId = Guid.NewGuid();
        Guid resourceTenantId = Guid.NewGuid();
        Mock<ICurrentUser> user = AuthenticatedUser(activeTenantId: activeTenantId);
        user.Setup(u => u.GetTenantSnapshotAsync(resourceTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantCtxSnapshot?)null);
        ResourceRef resource = new ResourceRef(TenantId: resourceTenantId);

        // Act — cross-tenant enabled, so mismatch is allowed but snapshot returned null → false
        bool result = await _sut.AuthorizeAsync(user.Object, permissionCode, resource);

        // Assert
        result.Should().BeFalse();
        user.Verify(u => u.GetTenantSnapshotAsync(resourceTenantId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
