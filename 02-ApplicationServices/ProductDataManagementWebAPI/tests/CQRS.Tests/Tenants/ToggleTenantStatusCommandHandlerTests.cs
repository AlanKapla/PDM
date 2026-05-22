using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Tenants.ToggleTenantStatus;
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

public sealed class ToggleTenantStatusCommandHandlerTests
{
    private readonly Mock<IRepository<Tenant>> _tenantRepoMock = new();
    private readonly Mock<IReadRepository<User>> _userRepoMock = new();
    private readonly Mock<IRepository<TenantMember>> _tenantMemberRepoMock = new();
    private readonly Mock<IRepository<TenantPreferencesProfile>> _tenantPrefsRepoMock = new();
    private readonly Mock<IReadRepository<Role>> _roleRepoMock = new();
    private readonly Mock<IReadRepository<Notification>> _notificationRepoMock = new();
    private readonly Mock<INotificationSender> _notificationSenderMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly ToggleTenantStatusCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public ToggleTenantStatusCommandHandlerTests()
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
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<Func<IQueryable<User>, IIncludableQueryable<User, object>>[]>()))
            .ReturnsAsync(new List<User>());

        _handler = new ToggleTenantStatusCommandHandler(
            _tenantRepoMock.Object,
            _userRepoMock.Object,
            _tenantMemberRepoMock.Object,
            _tenantPrefsRepoMock.Object,
            _roleRepoMock.Object,
            _notificationRepoMock.Object,
            _notificationSenderMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ToggleTenantStatusCommand ValidCommand(Guid tenantId, bool isActive) =>
        new ToggleTenantStatusCommand
        {
            TenantId = tenantId,
            IsActive = isActive
        };

    private static Tenant BuildTenant(Guid id, bool isActive = true) => new Tenant
    {
        Id = id,
        Name = "Test Tenant",
        IsActive = isActive
    };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenTenantNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _tenantRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Tenant, bool>>>(),
                It.IsAny<Func<IQueryable<Tenant>, IIncludableQueryable<Tenant, object>>[]>()))
            .ReturnsAsync((Tenant?)null);

        ToggleTenantStatusCommand command = ValidCommand(Guid.NewGuid(), true);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenActivatingTenant_SetsIsActiveAndReturnsUnit()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Tenant tenant = BuildTenant(tenantId, isActive: false);

        _tenantRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Tenant, bool>>>(),
                It.IsAny<Func<IQueryable<Tenant>, IIncludableQueryable<Tenant, object>>[]>()))
            .ReturnsAsync(tenant);

        _tenantMemberRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<TenantMember, bool>>>(),
                It.IsAny<Func<IQueryable<TenantMember>, IIncludableQueryable<TenantMember, object>>[]>()))
            .ReturnsAsync(new List<TenantMember>());

        ToggleTenantStatusCommand command = ValidCommand(tenantId, isActive: true);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        tenant.IsActive.Should().BeTrue();
        _tenantRepoMock.Verify(r => r.Update(tenant), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDeactivatingTenant_SetsIsInactiveAndReturnsUnit()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Tenant tenant = BuildTenant(tenantId, isActive: true);

        _tenantRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Tenant, bool>>>(),
                It.IsAny<Func<IQueryable<Tenant>, IIncludableQueryable<Tenant, object>>[]>()))
            .ReturnsAsync(tenant);

        _tenantMemberRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<TenantMember, bool>>>(),
                It.IsAny<Func<IQueryable<TenantMember>, IIncludableQueryable<TenantMember, object>>[]>()))
            .ReturnsAsync(new List<TenantMember>());

        _roleRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<Role, bool>>>(),
                It.IsAny<Func<IQueryable<Role>, IIncludableQueryable<Role, object>>[]>()))
            .ReturnsAsync(new List<Role>());

        _tenantPrefsRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<TenantPreferencesProfile, bool>>>(),
                It.IsAny<Func<IQueryable<TenantPreferencesProfile>, IIncludableQueryable<TenantPreferencesProfile, object>>[]>()))
            .ReturnsAsync(new List<TenantPreferencesProfile>());

        ToggleTenantStatusCommand command = ValidCommand(tenantId, isActive: false);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        tenant.IsActive.Should().BeFalse();
        _tenantRepoMock.Verify(r => r.Update(tenant), Times.Once);
    }
}
