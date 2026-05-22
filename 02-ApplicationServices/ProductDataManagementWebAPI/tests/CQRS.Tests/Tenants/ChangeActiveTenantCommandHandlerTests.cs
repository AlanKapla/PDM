using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using CQRS.Tenants.ChangeActiveTenant;
using Entities.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Tenants;

public sealed class ChangeActiveTenantCommandHandlerTests
{
    private readonly Mock<IRepository<TenantPreferencesProfile>> _tenantPrefsRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly ChangeActiveTenantCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public ChangeActiveTenantCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_userId);

        _handler = new ChangeActiveTenantCommandHandler(
            _tenantPrefsRepoMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ChangeActiveTenantCommand ValidCommand(Guid? tenantId = null) => new ChangeActiveTenantCommand
    {
        TenantId = tenantId ?? Guid.NewGuid()
    };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenProfileDoesNotExist_InsertsProfileAndReturnsActiveTenant()
    {
        // Arrange
        _tenantPrefsRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantPreferencesProfile, bool>>>(),
                It.IsAny<Func<IQueryable<TenantPreferencesProfile>, IIncludableQueryable<TenantPreferencesProfile, object>>[]>()))
            .ReturnsAsync((TenantPreferencesProfile?)null);

        ChangeActiveTenantCommand command = ValidCommand();

        // Act
        ActiveTenantWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ActiveTenantId.Should().Be(command.TenantId);
        _tenantPrefsRepoMock.Verify(r => r.Insert(It.Is<TenantPreferencesProfile>(p => p.ActiveTenantId == command.TenantId && p.UserId == _userId)), Times.Once);
        _tenantPrefsRepoMock.Verify(r => r.Update(It.IsAny<TenantPreferencesProfile>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProfileExists_UpdatesProfileAndReturnsActiveTenant()
    {
        // Arrange
        TenantPreferencesProfile existingProfile = new TenantPreferencesProfile
        {
            UserId = _userId,
            ActiveTenantId = Guid.NewGuid()
        };

        _tenantPrefsRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantPreferencesProfile, bool>>>(),
                It.IsAny<Func<IQueryable<TenantPreferencesProfile>, IIncludableQueryable<TenantPreferencesProfile, object>>[]>()))
            .ReturnsAsync(existingProfile);

        ChangeActiveTenantCommand command = ValidCommand();

        // Act
        ActiveTenantWeb result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ActiveTenantId.Should().Be(command.TenantId);
        existingProfile.ActiveTenantId.Should().Be(command.TenantId);
        _tenantPrefsRepoMock.Verify(r => r.Update(existingProfile), Times.Once);
        _tenantPrefsRepoMock.Verify(r => r.Insert(It.IsAny<TenantPreferencesProfile>()), Times.Never);
    }
}
