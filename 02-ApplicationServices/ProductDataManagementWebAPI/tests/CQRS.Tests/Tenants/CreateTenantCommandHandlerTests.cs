using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Tenants;
using CQRS.Tenants.CreateTenant;
using Entities.Enums;
using Entities.Models;
using Entities.Models.Roles;
using Entities.Models.Subscriptions;
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
    private readonly Mock<IReadRepository<SubscriptionPlanDefinition>> _planDefinitionRepoMock = new();
    private readonly Mock<IRepository<TenantSubscription>> _tenantSubscriptionRepoMock = new();
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
            _currentUserMock.Object,
            _planDefinitionRepoMock.Object,
            _tenantSubscriptionRepoMock.Object);
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

    private static SubscriptionPlanDefinition BuildFreePlanDefinition() => new SubscriptionPlanDefinition
    {
        Id = Guid.NewGuid(),
        Plan = SubscriptionPlan.Free,
        Name = "Free",
        MaxProjects = 1,
        MaxUsers = 5,
        Price = 0,
        Currency = "PLN",
        IsActive = true
    };

    private void SetupAdminRole() =>
        _roleRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Role, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildAdminRole());

    private void SetupFreePlan(SubscriptionPlanDefinition? plan) =>
        _planDefinitionRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<SubscriptionPlanDefinition, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

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
        SetupAdminRole();
        SetupFreePlan(null);

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
        SetupAdminRole();
        SetupFreePlan(null);

        TenantPreferencesProfile existingPrefs = new TenantPreferencesProfile
        {
            UserId = _userId,
            ActiveTenantId = Guid.NewGuid()
        };

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

    [Fact]
    public async Task Handle_WhenFreePlanExists_CreatesSubscriptionWithFullAccess()
    {
        // Arrange
        SetupAdminRole();
        SetupFreePlan(BuildFreePlanDefinition());

        TenantSubscription? capturedSubscription = null;
        _tenantSubscriptionRepoMock
            .Setup(r => r.Insert(It.IsAny<TenantSubscription>()))
            .Callback<TenantSubscription>(s => capturedSubscription = s)
            .Returns(Task.CompletedTask);

        CreateTenantCommand command = ValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _tenantSubscriptionRepoMock.Verify(r => r.Insert(It.IsAny<TenantSubscription>()), Times.Once);
        capturedSubscription.Should().NotBeNull();
        capturedSubscription!.IsFullAccess.Should().BeTrue();
        capturedSubscription.Plan.Should().Be(SubscriptionPlan.Free);
        capturedSubscription.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task Handle_WhenFreePlanNotFound_SkipsSubscriptionCreation()
    {
        // Arrange
        SetupAdminRole();
        SetupFreePlan(null);

        CreateTenantCommand command = ValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _tenantSubscriptionRepoMock.Verify(r => r.Insert(It.IsAny<TenantSubscription>()), Times.Never);
        _tenantSubscriptionRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenFreePlanExists_SubscriptionBelongsToCreatedTenant()
    {
        // Arrange
        SetupAdminRole();
        SetupFreePlan(BuildFreePlanDefinition());

        Guid capturedTenantId = Guid.Empty;
        TenantSubscription? capturedSubscription = null;

        _tenantRepoMock
            .Setup(r => r.Insert(It.IsAny<Tenant>()))
            .Callback<Tenant>(t => capturedTenantId = t.Id)
            .Returns(Task.CompletedTask);

        _tenantSubscriptionRepoMock
            .Setup(r => r.Insert(It.IsAny<TenantSubscription>()))
            .Callback<TenantSubscription>(s => capturedSubscription = s)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        capturedSubscription!.TenantId.Should().Be(capturedTenantId);
    }
}
