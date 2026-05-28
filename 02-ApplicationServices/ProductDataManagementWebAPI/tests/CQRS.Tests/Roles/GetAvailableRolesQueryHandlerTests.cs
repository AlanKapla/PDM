using Business.Interfaces.WebModels.Roles;
using CQRS.Roles.GetAvailableRoles;
using Entities.Enums;
using Entities.Models.Roles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Roles;

public sealed class GetAvailableRolesQueryHandlerTests
{
    private readonly Mock<IReadRepository<Role>> _roleRepoMock = new();
    private readonly GetAvailableRolesQueryHandler _handler;

    public GetAvailableRolesQueryHandlerTests()
    {
        _handler = new GetAvailableRolesQueryHandler(_roleRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenRolesExistForScope_ReturnsRolesOrderedByName()
    {
        // Arrange
        List<Role> roles = new()
        {
            new Role { Id = Guid.NewGuid(), Code = "TENANT.MEMBER", Name = "Member", Scope = RoleScope.Tenant, IsActive = true },
            new Role { Id = Guid.NewGuid(), Code = "TENANT.ADMIN", Name = "Admin", Scope = RoleScope.Tenant, IsActive = true }
        };

        _roleRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<Role, bool>>>(),
                It.IsAny<Func<IQueryable<Role>, IIncludableQueryable<Role, object>>[]>()))
            .ReturnsAsync(roles);

        GetAvailableRolesQuery query = new(RoleScope.Tenant);

        // Act
        IEnumerable<RoleWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        List<RoleWeb> resultList = result.ToList();
        resultList.Should().HaveCount(2);
        resultList[0].Name.Should().Be("Admin");
        resultList[1].Name.Should().Be("Member");
    }

    [Fact]
    public async Task Handle_WhenNoRolesForScope_ReturnsEmpty()
    {
        // Arrange
        _roleRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<Role, bool>>>(),
                It.IsAny<Func<IQueryable<Role>, IIncludableQueryable<Role, object>>[]>()))
            .ReturnsAsync(new List<Role>());

        GetAvailableRolesQuery query = new(RoleScope.Tenant);

        // Act
        IEnumerable<RoleWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenRolesExist_MapsFieldsCorrectly()
    {
        // Arrange
        Guid roleId = Guid.NewGuid();
        List<Role> roles = new()
        {
            new Role
            {
                Id = roleId,
                Code = "TENANT.ADMIN",
                Name = "Admin",
                Description = "Full access",
                Scope = RoleScope.Tenant,
                IsActive = true
            }
        };

        _roleRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<Role, bool>>>(),
                It.IsAny<Func<IQueryable<Role>, IIncludableQueryable<Role, object>>[]>()))
            .ReturnsAsync(roles);

        GetAvailableRolesQuery query = new(RoleScope.Tenant);

        // Act
        IEnumerable<RoleWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        RoleWeb roleWeb = result.Single();
        roleWeb.Id.Should().Be(roleId);
        roleWeb.Code.Should().Be("TENANT.ADMIN");
        roleWeb.Name.Should().Be("Admin");
        roleWeb.Description.Should().Be("Full access");
        roleWeb.Scope.Should().Be(RoleScope.Tenant);
    }
}
