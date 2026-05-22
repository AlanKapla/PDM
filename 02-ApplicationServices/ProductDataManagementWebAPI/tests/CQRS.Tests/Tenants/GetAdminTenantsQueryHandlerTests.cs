using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using CQRS.Tenants.GetAdminTenants;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Tenants;

public sealed class GetAdminTenantsQueryHandlerTests
{
    private readonly Mock<IRepository<TenantMember>> _tenantMemberRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetAdminTenantsQueryHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public GetAdminTenantsQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_userId);

        _handler = new GetAdminTenantsQueryHandler(
            _tenantMemberRepoMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static GetAdminTenantsQuery ValidQuery() => new GetAdminTenantsQuery();

    private static TenantMember BuildAdminMember(Guid userId, string tenantName) => new TenantMember
    {
        UserId = userId,
        IsActive = true,
        MemberRole = new Role { Code = RoleCodes.TenantAdmin },
        Tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = tenantName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        }
    };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoAdminMemberships_ReturnsEmptyList()
    {
        // Arrange
        _tenantMemberRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<TenantMember, bool>>>(),
                It.IsAny<Func<IQueryable<TenantMember>, IIncludableQueryable<TenantMember, object>>[]>()))
            .ReturnsAsync(new List<TenantMember>());

        GetAdminTenantsQuery query = ValidQuery();

        // Act
        IEnumerable<TenantBasicWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenAdminMembershipsExist_ReturnsMappedOrderedList()
    {
        // Arrange
        TenantMember memberB = BuildAdminMember(_userId, "Beta Corp");
        TenantMember memberA = BuildAdminMember(_userId, "Alpha Corp");

        _tenantMemberRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<TenantMember, bool>>>(),
                It.IsAny<Func<IQueryable<TenantMember>, IIncludableQueryable<TenantMember, object>>[]>()))
            .ReturnsAsync(new List<TenantMember> { memberB, memberA });

        GetAdminTenantsQuery query = ValidQuery();

        // Act
        IEnumerable<TenantBasicWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        List<TenantBasicWeb> list = result.ToList();
        list.Should().HaveCount(2);
        list[0].Name.Should().Be("Alpha Corp");
        list[1].Name.Should().Be("Beta Corp");
        list[0].RoleCode.Should().Be(RoleCodes.TenantAdmin);
    }
}
