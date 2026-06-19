using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using CQRS.Tenants.RemoveTenantInvitation;
using Entities.Models.Tenants;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Tenants;

public sealed class RemoveTenantInvitationCommandHandlerTests
{
    private readonly Mock<IReadRepository<Tenant>> _tenantRepoMock = new();
    private readonly Mock<IRepository<TenantInvitation>> _invitationRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<RemoveTenantInvitationCommandHandler>> _loggerMock = new();
    private readonly RemoveTenantInvitationCommandHandler _handler;

    public RemoveTenantInvitationCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _handler = new RemoveTenantInvitationCommandHandler(
            _tenantRepoMock.Object,
            _invitationRepoMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    private static RemoveTenantInvitationCommand ValidCommand(Guid tenantId, Guid invitationId) =>
        new RemoveTenantInvitationCommand
        {
            TenantId = tenantId,
            InvitationId = invitationId
        };

    private static Tenant BuildTenant(Guid id) => new Tenant
    {
        Id = id,
        Name = "Test Tenant",
        IsActive = true
    };

    private static TenantInvitation BuildInvitation(Guid id, Guid tenantId) => new TenantInvitation
    {
        Id = id,
        TenantId = tenantId,
        Email = "user@test.com",
        Token = "token",
        IsActive = true,
        Status = InvitationStatus.Pending,
        InvitedByUserId = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };

    [Fact]
    public async Task Handle_WhenTenantNotFound_ThrowsNotFoundApiException()
    {
        _tenantRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Tenant, bool>>>()))
            .ReturnsAsync((Tenant?)null);

        RemoveTenantInvitationCommand command = ValidCommand(Guid.NewGuid(), Guid.NewGuid());

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenInvitationNotFound_ThrowsNotFoundApiException()
    {
        Guid tenantId = Guid.NewGuid();
        Tenant tenant = BuildTenant(tenantId);

        _tenantRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Tenant, bool>>>()))
            .ReturnsAsync(tenant);

        _invitationRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantInvitation, bool>>>(),
                It.IsAny<Func<IQueryable<TenantInvitation>, IIncludableQueryable<TenantInvitation, object>>[]>()))
            .ReturnsAsync((TenantInvitation?)null);

        RemoveTenantInvitationCommand command = ValidCommand(tenantId, Guid.NewGuid());

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenInvitationExists_RevokesInvitationAndReturnsUnit()
    {
        Guid tenantId = Guid.NewGuid();
        Guid invitationId = Guid.NewGuid();
        Tenant tenant = BuildTenant(tenantId);
        TenantInvitation invitation = BuildInvitation(invitationId, tenantId);

        _tenantRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Tenant, bool>>>()))
            .ReturnsAsync(tenant);

        _invitationRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantInvitation, bool>>>(),
                It.IsAny<Func<IQueryable<TenantInvitation>, IIncludableQueryable<TenantInvitation, object>>[]>()))
            .ReturnsAsync(invitation);

        RemoveTenantInvitationCommand command = ValidCommand(tenantId, invitationId);

        Unit result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        invitation.IsActive.Should().BeFalse();
        invitation.Status.Should().Be(InvitationStatus.Revoked);
        _invitationRepoMock.Verify(r => r.Update(invitation), Times.Once);
    }
}
