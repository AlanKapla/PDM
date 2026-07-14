using Business.Implementation.Services;
using Business.Interfaces.Model;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Business.Tests.Services;

public class InMemoryUserContextCacheTests : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly InMemoryUserContextCache _sut;

    public InMemoryUserContextCacheTests()
    {
        _memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        _sut = new InMemoryUserContextCache(_memoryCache);
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
    }

    // ─── GetOrCreateUserPermissionsVersionAsync ───────────────────────────────

    [Fact]
    public async Task GetOrCreateUserPermissionsVersionAsync_CacheMiss_CallsFactory()
    {
        Guid userId = Guid.NewGuid();
        bool factoryCalled = false;

        int result = await _sut.GetOrCreateUserPermissionsVersionAsync(
            userId,
            factory: () => { factoryCalled = true; return Task.FromResult(5); },
            ttl: TimeSpan.FromMinutes(10),
            cancellationToken: CancellationToken.None);

        factoryCalled.Should().BeTrue();
        result.Should().Be(5);
    }

    [Fact]
    public async Task GetOrCreateUserPermissionsVersionAsync_CacheHit_DoesNotCallFactory()
    {
        Guid userId = Guid.NewGuid();
        int callCount = 0;

        // First call — populates cache
        await _sut.GetOrCreateUserPermissionsVersionAsync(
            userId,
            factory: () => { callCount++; return Task.FromResult(7); },
            ttl: TimeSpan.FromMinutes(10),
            cancellationToken: CancellationToken.None);

        // Second call — should hit cache
        int result = await _sut.GetOrCreateUserPermissionsVersionAsync(
            userId,
            factory: () => { callCount++; return Task.FromResult(99); },
            ttl: TimeSpan.FromMinutes(10),
            cancellationToken: CancellationToken.None);

        callCount.Should().Be(1);
        result.Should().Be(7);
    }

    // ─── GetOrCreateRolePermissionsAsync ─────────────────────────────────────

    [Fact]
    public async Task GetOrCreateRolePermissionsAsync_CacheMiss_CallsFactory()
    {
        Guid roleId = Guid.NewGuid();
        HashSet<string> permissions = new HashSet<string> { "PERM.A", "PERM.B" };

        HashSet<string> result = await _sut.GetOrCreateRolePermissionsAsync(
            roleId,
            factory: () => Task.FromResult(permissions),
            ttl: TimeSpan.FromMinutes(10),
            cancellationToken: CancellationToken.None);

        result.Should().BeEquivalentTo(permissions);
    }

    [Fact]
    public async Task GetOrCreateRolePermissionsAsync_CacheHit_ReturnsCachedValue()
    {
        Guid roleId = Guid.NewGuid();
        int callCount = 0;

        await _sut.GetOrCreateRolePermissionsAsync(
            roleId,
            factory: () => { callCount++; return Task.FromResult(new HashSet<string> { "P1" }); },
            ttl: TimeSpan.FromMinutes(10),
            cancellationToken: CancellationToken.None);

        HashSet<string> result = await _sut.GetOrCreateRolePermissionsAsync(
            roleId,
            factory: () => { callCount++; return Task.FromResult(new HashSet<string> { "P2" }); },
            ttl: TimeSpan.FromMinutes(10),
            cancellationToken: CancellationToken.None);

        callCount.Should().Be(1);
        result.Should().Contain("P1");
    }

    // ─── GetOrCreateTenantCtxAsync ────────────────────────────────────────────

    private static TenantCtxSnapshot BuildTenantCtx(Guid tenantId) =>
        new TenantCtxSnapshot(
            TenantId: tenantId,
            IsAdmin: false,
            IsActive: true);

    private static ProjectCtxSnapshot BuildProjectCtx(Guid projectId, Guid tenantId) =>
        new ProjectCtxSnapshot(
            ProjectId: projectId,
            TenantId: tenantId,
            ProjectPermissionCodes: new HashSet<string> { "PROJECT.VIEW" },
            IsProjectAdmin: false,
            IsActive: true);

    [Fact]
    public async Task GetOrCreateTenantCtxAsync_CacheMiss_CallsFactory()
    {
        Guid userId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        TenantCtxSnapshot expected = BuildTenantCtx(tenantId);
        bool factoryCalled = false;

        TenantCtxSnapshot result = await _sut.GetOrCreateTenantCtxAsync(
            userId, tenantId,
            version: 1,
            factory: () => { factoryCalled = true; return Task.FromResult(expected); },
            ttl: TimeSpan.FromMinutes(10),
            cancellationToken: CancellationToken.None);

        factoryCalled.Should().BeTrue();
        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetOrCreateTenantCtxAsync_DifferentVersions_CallsFactoryEachTime()
    {
        Guid userId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        int callCount = 0;
        TenantCtxSnapshot ctx = BuildTenantCtx(tenantId);

        await _sut.GetOrCreateTenantCtxAsync(
            userId, tenantId, version: 1,
            factory: () => { callCount++; return Task.FromResult(ctx); },
            ttl: TimeSpan.FromMinutes(10),
            cancellationToken: CancellationToken.None);

        await _sut.GetOrCreateTenantCtxAsync(
            userId, tenantId, version: 2,
            factory: () => { callCount++; return Task.FromResult(ctx); },
            ttl: TimeSpan.FromMinutes(10),
            cancellationToken: CancellationToken.None);

        callCount.Should().Be(2);
    }

    // ─── GetOrCreateProjectCtxAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetOrCreateProjectCtxAsync_CacheMiss_CallsFactory()
    {
        Guid userId = Guid.NewGuid();
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        ProjectCtxSnapshot expected = BuildProjectCtx(projectId, tenantId);
        bool factoryCalled = false;

        ProjectCtxSnapshot result = await _sut.GetOrCreateProjectCtxAsync(
            userId, tenantId, projectId,
            version: 1,
            factory: () => { factoryCalled = true; return Task.FromResult(expected); },
            ttl: TimeSpan.FromMinutes(10),
            cancellationToken: CancellationToken.None);

        factoryCalled.Should().BeTrue();
        result.Should().Be(expected);
    }

    // ─── Invalidation ─────────────────────────────────────────────────────────

    [Fact]
    public async Task InvalidateUserPermissionsVersion_RemovesFromCache()
    {
        Guid userId = Guid.NewGuid();
        int callCount = 0;

        await _sut.GetOrCreateUserPermissionsVersionAsync(
            userId,
            factory: () => { callCount++; return Task.FromResult(10); },
            ttl: TimeSpan.FromMinutes(10),
            cancellationToken: CancellationToken.None);

        _sut.InvalidateUserPermissionsVersion(userId);

        // After invalidation, factory should be called again
        await _sut.GetOrCreateUserPermissionsVersionAsync(
            userId,
            factory: () => { callCount++; return Task.FromResult(20); },
            ttl: TimeSpan.FromMinutes(10),
            cancellationToken: CancellationToken.None);

        callCount.Should().Be(2);
    }

    [Fact]
    public async Task InvalidateRolePermissions_RemovesFromCache()
    {
        Guid roleId = Guid.NewGuid();
        int callCount = 0;

        await _sut.GetOrCreateRolePermissionsAsync(
            roleId,
            factory: () => { callCount++; return Task.FromResult(new HashSet<string> { "P1" }); },
            ttl: TimeSpan.FromMinutes(10),
            cancellationToken: CancellationToken.None);

        _sut.InvalidateRolePermissions(roleId);

        await _sut.GetOrCreateRolePermissionsAsync(
            roleId,
            factory: () => { callCount++; return Task.FromResult(new HashSet<string> { "P2" }); },
            ttl: TimeSpan.FromMinutes(10),
            cancellationToken: CancellationToken.None);

        callCount.Should().Be(2);
    }

    [Fact]
    public async Task InvalidateUserPermissionsVersion_DoesNotAffectOtherUser()
    {
        Guid userA = Guid.NewGuid();
        Guid userB = Guid.NewGuid();
        int callCountB = 0;

        await _sut.GetOrCreateUserPermissionsVersionAsync(
            userA, () => Task.FromResult(1),
            ttl: TimeSpan.FromMinutes(10), CancellationToken.None);

        await _sut.GetOrCreateUserPermissionsVersionAsync(
            userB, () => { callCountB++; return Task.FromResult(2); },
            ttl: TimeSpan.FromMinutes(10), CancellationToken.None);

        _sut.InvalidateUserPermissionsVersion(userA);

        await _sut.GetOrCreateUserPermissionsVersionAsync(
            userB, () => { callCountB++; return Task.FromResult(99); },
            ttl: TimeSpan.FromMinutes(10), CancellationToken.None);

        // userB cache not affected
        callCountB.Should().Be(1);
    }
}
