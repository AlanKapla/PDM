using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using CQRS.Tenants.GetTenantDetails;
using Entities.Models.Tenants;
using Entities.Models.Users;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Tenants;

public sealed class GetTenantDetailsQueryHandlerTests
{
    private readonly Mock<IReadRepository<Tenant>> _tenantRepoMock = new();
    private readonly Mock<IRepository<TenantMember>> _tenantMemberRepoMock = new();
    private readonly Mock<IReadRepository<TenantInvitation>> _invitationRepoMock = new();
    private readonly Mock<IReadRepository<User>> _userRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly GetTenantDetailsQueryHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public GetTenantDetailsQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_userId);

        _handler = new GetTenantDetailsQueryHandler(
            _tenantRepoMock.Object,
            _tenantMemberRepoMock.Object,
            _invitationRepoMock.Object,
            _userRepoMock.Object,
            _currentUserMock.Object);
    }

    private static GetTenantDetailsQuery ValidQuery(Guid tenantId) => new GetTenantDetailsQuery
    {
        TenantId = tenantId
    };

    private static Tenant BuildTenant(Guid id) => new Tenant
    {
        Id = id,
        Name = "Test Tenant",
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    private void SetupEmptyCollections()
    {
        _tenantMemberRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<TenantMember, bool>>>(),
                It.IsAny<Func<IQueryable<TenantMember>, IIncludableQueryable<TenantMember, object>>[]>()))
            .ReturnsAsync(new List<TenantMember>());

        _userRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync(new List<User>());

        _invitationRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<TenantInvitation, bool>>>(),
                It.IsAny<Func<IQueryable<TenantInvitation>, IIncludableQueryable<TenantInvitation, object>>[]>()))
            .ReturnsAsync(new List<TenantInvitation>());
    }

    [Fact]
    public async Task Handle_WhenTenantNotFound_ThrowsNotFoundApiException()
    {
        _tenantRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Tenant, bool>>>()))
            .ReturnsAsync((Tenant?)null);

        GetTenantDetailsQuery query = ValidQuery(Guid.NewGuid());

        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenTenantExists_ReturnsTenantDetailsWeb()
    {
        Guid tenantId = Guid.NewGuid();
        Tenant tenant = BuildTenant(tenantId);
        SetupEmptyCollections();

        _tenantRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Tenant, bool>>>()))
            .ReturnsAsync(tenant);

        GetTenantDetailsQuery query = ValidQuery(tenantId);

        TenantDetailsWeb result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(tenantId);
        result.Name.Should().Be("Test Tenant");
    }
}
