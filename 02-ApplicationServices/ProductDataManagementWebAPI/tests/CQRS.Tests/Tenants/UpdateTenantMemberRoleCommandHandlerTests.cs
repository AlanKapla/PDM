using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Tenants.UpdateTenantMemberRole;
using Entities.Enums;
using Entities.Models;
using Entities.Models.Notifications;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Tenants;

public sealed class UpdateTenantMemberRoleCommandHandlerTests
{
    private readonly Mock<IReadRepository<Tenant>> _tenantRepoMock = new();
    private readonly Mock<IReadRepository<User>> _userRepoMock = new();
    private readonly Mock<IRepository<TenantMember>> _tenantMemberRepoMock = new();
    private readonly Mock<IRepository<TenantPreferencesProfile>> _tenantPrefsRepoMock = new();
    private readonly Mock<IReadRepository<Role>> _roleRepoMock = new();
    private readonly Mock<IReadRepository<Notification>> _notificationRepoMock = new();
    private readonly Mock<IPermissionsVersionService> _permissionsVersionServiceMock = new();
    private readonly Mock<INotificationSender> _notificationSenderMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly UpdateTenantMemberRoleCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public UpdateTenantMemberRoleCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_userId);

        _permissionsVersionServiceMock
            .Setup(s => s.BumpVersionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _notificationRepoMock
            .Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<Notification, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _notificationSenderMock
            .Setup(s => s.EnqueueAsync(It.IsAny<NotificationPayloadDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);

        _handler = new UpdateTenantMemberRoleCommandHandler(
            _tenantRepoMock.Object,
            _userRepoMock.Object,
            _tenantMemberRepoMock.Object,
            _tenantPrefsRepoMock.Object,
            _roleRepoMock.Object,
            _notificationRepoMock.Object,
            _permissionsVersionServiceMock.Object,
            _notificationSenderMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static UpdateTenantMemberRoleCommand ValidCommand(Guid tenantId, Guid userId, Guid roleId) =>
        new UpdateTenantMemberRoleCommand
        {
            TenantId = tenantId,
            UserId = userId,
            RoleId = roleId
        };

    private static Tenant BuildTenant(Guid id) => new Tenant
    {
        Id = id,
        Name = "Test Tenant",
        IsActive = true
    };

    private static TenantMember BuildMember(Guid tenantId, Guid userId, Role? role = null) => new TenantMember
    {
        TenantId = tenantId,
        UserId = userId,
        IsActive = true,
        RoleId = role?.Id,
        MemberRole = role
    };

    private static Role BuildRole(Guid id, string code) => new Role
    {
        Id = id,
        Code = code,
        Name = code,
        Scope = RoleScope.Tenant,
        IsActive = true
    };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenTenantNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _tenantRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Tenant, bool>>>()))
            .ReturnsAsync((Tenant?)null);

        UpdateTenantMemberRoleCommand command = ValidCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenMemberNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Tenant tenant = BuildTenant(tenantId);

        _tenantRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Tenant, bool>>>()))
            .ReturnsAsync(tenant);

        _tenantMemberRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantMember, bool>>>(),
                It.IsAny<Func<IQueryable<TenantMember>, IIncludableQueryable<TenantMember, object>>[]>()))
            .ReturnsAsync((TenantMember?)null);

        UpdateTenantMemberRoleCommand command = ValidCommand(tenantId, Guid.NewGuid(), Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenRoleNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid targetUserId = Guid.NewGuid();
        Tenant tenant = BuildTenant(tenantId);
        TenantMember member = BuildMember(tenantId, targetUserId);

        _tenantRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Tenant, bool>>>()))
            .ReturnsAsync(tenant);

        _tenantMemberRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantMember, bool>>>(),
                It.IsAny<Func<IQueryable<TenantMember>, IIncludableQueryable<TenantMember, object>>[]>()))
            .ReturnsAsync(member);

        _roleRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Role, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        UpdateTenantMemberRoleCommand command = ValidCommand(tenantId, targetUserId, Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenDemotingLastAdmin_ThrowsConflictApiException()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid targetUserId = Guid.NewGuid();
        Guid newRoleId = Guid.NewGuid();
        Tenant tenant = BuildTenant(tenantId);
        Role adminRole = BuildRole(Guid.NewGuid(), RoleCodes.TenantAdmin);
        Role memberRole = BuildRole(newRoleId, RoleCodes.TenantMember);
        TenantMember member = BuildMember(tenantId, targetUserId, adminRole);

        _tenantRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Tenant, bool>>>()))
            .ReturnsAsync(tenant);

        _tenantMemberRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantMember, bool>>>(),
                It.IsAny<Func<IQueryable<TenantMember>, IIncludableQueryable<TenantMember, object>>[]>()))
            .ReturnsAsync(member);

        _roleRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Role, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(memberRole);

        _tenantMemberRepoMock
            .Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<TenantMember, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // Only one admin

        UpdateTenantMemberRoleCommand command = ValidCommand(tenantId, targetUserId, newRoleId);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictApiException>();
    }

    [Fact]
    public async Task Handle_WhenRoleUpdated_BumpsPermissionsAndReturnsUnit()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid targetUserId = Guid.NewGuid();
        Guid newRoleId = Guid.NewGuid();
        Tenant tenant = BuildTenant(tenantId);
        Role currentRole = BuildRole(Guid.NewGuid(), RoleCodes.TenantMember);
        Role newRole = BuildRole(newRoleId, RoleCodes.TenantAdmin);
        TenantMember member = BuildMember(tenantId, targetUserId, currentRole);

        _tenantRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Tenant, bool>>>()))
            .ReturnsAsync(tenant);

        _tenantMemberRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantMember, bool>>>(),
                It.IsAny<Func<IQueryable<TenantMember>, IIncludableQueryable<TenantMember, object>>[]>()))
            .ReturnsAsync(member);

        _roleRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Role, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(newRole);

        _tenantPrefsRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantPreferencesProfile, bool>>>(),
                It.IsAny<Func<IQueryable<TenantPreferencesProfile>, IIncludableQueryable<TenantPreferencesProfile, object>>[]>()))
            .ReturnsAsync((TenantPreferencesProfile?)null);

        UpdateTenantMemberRoleCommand command = ValidCommand(tenantId, targetUserId, newRoleId);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        member.RoleId.Should().Be(newRoleId);
        _tenantMemberRepoMock.Verify(r => r.Update(member), Times.Once);
        _permissionsVersionServiceMock.Verify(s => s.BumpVersionAsync(targetUserId, It.IsAny<CancellationToken>()), Times.Once);
        _notificationSenderMock.Verify(s => s.EnqueueAsync(It.IsAny<NotificationPayloadDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
