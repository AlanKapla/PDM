using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using CQRS.Tenants.GetUserTenants;
using Entities.Models;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Tenants;

public sealed class GetUserTenantsQueryHandlerTests
{
    private readonly Mock<IReadRepository<Tenant>> _tenantRepoMock = new();
    private readonly Mock<IRepository<TenantMember>> _tenantMemberRepoMock = new();
    private readonly Mock<IReadRepository<TenantPreferencesProfile>> _preferencesRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetUserTenantsQueryHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public GetUserTenantsQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_userId);
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(false);

        _handler = new GetUserTenantsQueryHandler(
            _tenantRepoMock.Object,
            _tenantMemberRepoMock.Object,
            _preferencesRepoMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static GetUserTenantsQuery ValidQuery() => new GetUserTenantsQuery();

    private static TenantMember BuildActiveMembership(Guid userId, Guid tenantId, string tenantName, string roleCode) =>
        new TenantMember
        {
            TenantId = tenantId,
            UserId = userId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Tenant = new Tenant { Id = tenantId, Name = tenantName, IsActive = true, CreatedAt = DateTime.UtcNow },
            MemberRole = new Role { Code = roleCode }
        };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenRegularUserWithMemberships_ReturnsTenantList()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        TenantMember membership = BuildActiveMembership(_userId, tenantId, "My Tenant", RoleCodes.TenantMember);

        _preferencesRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantPreferencesProfile, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPreferencesProfile?)null);

        _tenantMemberRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<TenantMember, bool>>>(),
                It.IsAny<Func<IQueryable<TenantMember>, IIncludableQueryable<TenantMember, object>>[]>()))
            .ReturnsAsync(new List<TenantMember> { membership });

        GetUserTenantsQuery query = ValidQuery();

        // Act
        IEnumerable<UserTenantWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        List<UserTenantWeb> list = result.ToList();
        list.Should().HaveCount(1);
        list[0].Id.Should().Be(tenantId);
        list[0].Name.Should().Be("My Tenant");
        list[0].RoleCode.Should().Be(RoleCodes.TenantMember);
        list[0].IsActiveTenant.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenSuperAdmin_ReturnsAllTenants()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsSuperAdmin).Returns(true);

        Guid tenantId = Guid.NewGuid();
        Tenant tenant = new Tenant { Id = tenantId, Name = "All Tenants Corp", IsActive = true, CreatedAt = DateTime.UtcNow };

        _preferencesRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantPreferencesProfile, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPreferencesProfile?)null);

        _tenantRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<Tenant, bool>>>(),
                It.IsAny<Func<IQueryable<Tenant>, IIncludableQueryable<Tenant, object>>[]>()))
            .ReturnsAsync(new List<Tenant> { tenant });

        _tenantMemberRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<TenantMember, bool>>>(),
                It.IsAny<Func<IQueryable<TenantMember>, IIncludableQueryable<TenantMember, object>>[]>()))
            .ReturnsAsync(new List<TenantMember>());

        GetUserTenantsQuery query = ValidQuery();

        // Act
        IEnumerable<UserTenantWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        List<UserTenantWeb> list = result.ToList();
        list.Should().HaveCount(1);
        list[0].Id.Should().Be(tenantId);
        list[0].RoleCode.Should().Be(RoleCodes.SystemSuperAdmin);
    }
}
