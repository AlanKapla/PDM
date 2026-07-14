using Business.Interfaces.WebModels.Tenants;
using CQRS.Tenants.GetTenantMembers;
using Entities.Models.Tenants;
using Entities.Models.Users;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Tenants;

public sealed class GetTenantMembersQueryHandlerTests
{
    private readonly Mock<IRepository<TenantMember>> _tenantMemberRepoMock = new();
    private readonly GetTenantMembersQueryHandler _handler;

    public GetTenantMembersQueryHandlerTests()
    {
        _handler = new GetTenantMembersQueryHandler(_tenantMemberRepoMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static GetTenantMembersQuery ValidQuery(Guid? tenantId = null) => new GetTenantMembersQuery
    {
        TenantId = tenantId ?? Guid.NewGuid()
    };

    private static TenantMember BuildMember(Guid tenantId, string email, string lastName) => new TenantMember
    {
        TenantId = tenantId,
        UserId = Guid.NewGuid(),
        IsActive = true,
        IsAdmin = false,
        CreatedAt = DateTime.UtcNow,
        User = new User { Email = email, FirstName = "Jan", LastName = lastName }
    };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoMembers_ReturnsEmptyCollection()
    {
        // Arrange
        _tenantMemberRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<TenantMember, bool>>>(),
                It.IsAny<Func<IQueryable<TenantMember>, IIncludableQueryable<TenantMember, object>>[]>()))
            .ReturnsAsync(new List<TenantMember>());

        GetTenantMembersQuery query = ValidQuery();

        // Act
        IEnumerable<TenantMemberWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenMembersExist_ReturnsMappedMembersOrderedByLastName()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        TenantMember memberZ = BuildMember(tenantId, "zebra@test.com", "Zebra");
        TenantMember memberA = BuildMember(tenantId, "apple@test.com", "Apple");

        _tenantMemberRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<TenantMember, bool>>>(),
                It.IsAny<Func<IQueryable<TenantMember>, IIncludableQueryable<TenantMember, object>>[]>()))
            .ReturnsAsync(new List<TenantMember> { memberZ, memberA });

        GetTenantMembersQuery query = ValidQuery(tenantId);

        // Act
        IEnumerable<TenantMemberWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        List<TenantMemberWeb> list = result.ToList();
        list.Should().HaveCount(2);
        list[0].LastName.Should().Be("Apple");
        list[1].LastName.Should().Be("Zebra");
        list[0].IsAdmin.Should().BeFalse();
    }
}
