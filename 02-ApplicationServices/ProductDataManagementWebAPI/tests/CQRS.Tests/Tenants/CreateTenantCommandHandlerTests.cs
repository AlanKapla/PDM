using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Tenants;
using CQRS.Tenants.CreateTenant;
using Entities.Enums;
using Entities.Models;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Tenants;

public sealed class CreateTenantCommandHandlerTests
{
    private readonly Mock<IReadRepository<Tenant>> _tenantRepoMock = new();
    private readonly Mock<IRepository<TenantMember>> _tenantMemberRepoMock = new();
    private readonly Mock<IRepository<TenantPreferencesProfile>> _tenantPrefsRepoMock = new();
    private readonly Mock<IReadRepository<Role>> _roleRepoMock = new();
    private readonly Mock<IPermissionsVersionService> _permissionsVersionServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly CreateTenantCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public CreateTenantCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_userId);

        _permissionsVersionServiceMock
            .Setup(s => s.BumpVersionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new CreateTenantCommandHandler(
            _tenantRepoMock.Object,
            _tenantMemberRepoMock.Object,
            _tenantPrefsRepoMock.Object,
            _roleRepoMock.Object,
            _permissionsVersionServiceMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static CreateTenantCommand ValidCommand() => new CreateTenantCommand
    {
        Name = "My New Tenant"
    };

    private static Role BuildAdminRole() => new Role
    {
        Id = Guid.NewGuid(),
        Code = RoleCodes.TenantAdmin,
        Scope = RoleScope.Tenant,
        IsActive = true
    };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenAdminRoleNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _roleRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Role, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        CreateTenantCommand command = ValidCommand();

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenPrefsDoNotExist_CreatesTenantAndInsertsMember()
    {
        // Arrange
        Role adminRole = BuildAdminRole();

        _roleRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Role, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminRole);

        _tenantPrefsRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantPreferencesProfile, bool>>>(),
                It.IsAny<Func<IQueryable<TenantPreferencesProfile>, IIncludableQueryable<TenantPreferencesProfile, object>>[]>()))
            .ReturnsAsync((TenantPreferencesProfile?)null);

        CreateTenantCommand command = ValidCommand();

        // Act
        TenantDetailsWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(command.Name);
        result.RoleCode.Should().Be(RoleCodes.TenantAdmin);
        result.IsActive.Should().BeTrue();

        _tenantRepoMock.Verify(r => r.Insert(It.IsAny<Tenant>()), Times.Once);
        _tenantMemberRepoMock.Verify(r => r.Insert(It.IsAny<TenantMember>()), Times.Once);
        _tenantPrefsRepoMock.Verify(r => r.Insert(It.IsAny<TenantPreferencesProfile>()), Times.Once);
        _permissionsVersionServiceMock.Verify(s => s.BumpVersionAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPrefsExist_CreatesTenantAndUpdatesPrefs()
    {
        // Arrange
        Role adminRole = BuildAdminRole();
        TenantPreferencesProfile existingPrefs = new TenantPreferencesProfile
        {
            UserId = _userId,
            ActiveTenantId = Guid.NewGuid()
        };

        _roleRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Role, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminRole);

        _tenantPrefsRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantPreferencesProfile, bool>>>(),
                It.IsAny<Func<IQueryable<TenantPreferencesProfile>, IIncludableQueryable<TenantPreferencesProfile, object>>[]>()))
            .ReturnsAsync(existingPrefs);

        CreateTenantCommand command = ValidCommand();

        // Act
        TenantDetailsWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(command.Name);

        _tenantPrefsRepoMock.Verify(r => r.Update(existingPrefs), Times.Once);
        _tenantPrefsRepoMock.Verify(r => r.Insert(It.IsAny<TenantPreferencesProfile>()), Times.Never);
    }
}
