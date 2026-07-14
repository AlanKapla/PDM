using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Tenants.RemoveTenantMember;
using Entities.Models;
using Entities.Models.Notifications;
using Entities.Models.Tenants;
using Entities.Models.Users;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Tenants;

public sealed class RemoveTenantMemberCommandHandlerTests
{
    private readonly Mock<IReadRepository<Tenant>> _tenantRepoMock = new();
    private readonly Mock<IReadRepository<User>> _userRepoMock = new();
    private readonly Mock<IRepository<TenantMember>> _tenantMemberRepoMock = new();
    private readonly Mock<IRepository<TenantPreferencesProfile>> _tenantPreferencesRepoMock = new();
    private readonly Mock<IReadRepository<Notification>> _notificationRepoMock = new();
    private readonly Mock<INotificationSender> _notificationSenderMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IProjectMembershipProvisioner> _projectMembershipProvisionerMock = new();
    private readonly RemoveTenantMemberCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public RemoveTenantMemberCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_userId);

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
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _projectMembershipProvisionerMock
            .Setup(p => p.DeactivateAllProjectMembershipsAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new RemoveTenantMemberCommandHandler(
            _tenantRepoMock.Object,
            _userRepoMock.Object,
            _tenantMemberRepoMock.Object,
            _tenantPreferencesRepoMock.Object,
            _notificationRepoMock.Object,
            _notificationSenderMock.Object,
            _currentUserMock.Object,
            _projectMembershipProvisionerMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static RemoveTenantMemberCommand ValidCommand(Guid tenantId, Guid userId) =>
        new RemoveTenantMemberCommand
        {
            TenantId = tenantId,
            UserId = userId
        };

    private static Tenant BuildTenant(Guid id) => new Tenant
    {
        Id = id,
        Name = "Test Tenant",
        IsActive = true
    };

    private static TenantMember BuildActiveMember(Guid tenantId, Guid userId) => new TenantMember
    {
        TenantId = tenantId,
        UserId = userId,
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

        RemoveTenantMemberCommand command = ValidCommand(Guid.NewGuid(), Guid.NewGuid());

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

        RemoveTenantMemberCommand command = ValidCommand(tenantId, Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenMemberExists_RemovesMemberAndReturnsUnit()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid targetUserId = Guid.NewGuid();
        Tenant tenant = BuildTenant(tenantId);
        TenantMember member = BuildActiveMember(tenantId, targetUserId);

        _tenantRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Tenant, bool>>>()))
            .ReturnsAsync(tenant);

        _tenantMemberRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantMember, bool>>>(),
                It.IsAny<Func<IQueryable<TenantMember>, IIncludableQueryable<TenantMember, object>>[]>()))
            .ReturnsAsync(member);

        _tenantPreferencesRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantPreferencesProfile, bool>>>(),
                It.IsAny<Func<IQueryable<TenantPreferencesProfile>, IIncludableQueryable<TenantPreferencesProfile, object>>[]>()))
            .ReturnsAsync((TenantPreferencesProfile?)null);

        RemoveTenantMemberCommand command = ValidCommand(tenantId, targetUserId);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        member.IsActive.Should().BeFalse();
        _tenantMemberRepoMock.Verify(r => r.Update(member), Times.Once);
        _projectMembershipProvisionerMock.Verify(
            p => p.DeactivateAllProjectMembershipsAsync(tenantId, targetUserId, It.IsAny<CancellationToken>()),
            Times.Once);
        _notificationSenderMock.Verify(s => s.EnqueueAsync(It.IsAny<NotificationPayloadDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMemberHasActiveTenantPrefs_ClearsActiveTenantId()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Guid targetUserId = Guid.NewGuid();
        Tenant tenant = BuildTenant(tenantId);
        TenantMember member = BuildActiveMember(tenantId, targetUserId);
        TenantPreferencesProfile prefs = new TenantPreferencesProfile
        {
            UserId = targetUserId,
            ActiveTenantId = tenantId
        };

        _tenantRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Tenant, bool>>>()))
            .ReturnsAsync(tenant);

        _tenantMemberRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantMember, bool>>>(),
                It.IsAny<Func<IQueryable<TenantMember>, IIncludableQueryable<TenantMember, object>>[]>()))
            .ReturnsAsync(member);

        _tenantPreferencesRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantPreferencesProfile, bool>>>(),
                It.IsAny<Func<IQueryable<TenantPreferencesProfile>, IIncludableQueryable<TenantPreferencesProfile, object>>[]>()))
            .ReturnsAsync(prefs);

        RemoveTenantMemberCommand command = ValidCommand(tenantId, targetUserId);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        prefs.ActiveTenantId.Should().BeNull();
        _tenantPreferencesRepoMock.Verify(r => r.Update(prefs), Times.Once);
    }
}
