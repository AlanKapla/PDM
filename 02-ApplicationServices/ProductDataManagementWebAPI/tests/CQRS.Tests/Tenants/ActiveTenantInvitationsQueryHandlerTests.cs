using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using CQRS.Tenants.ActiveInvitations;
using Entities.Models.Tenants;
using Entities.Models.Users;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Tenants;

public sealed class ActiveTenantInvitationsQueryHandlerTests
{
    private readonly Mock<IReadRepository<TenantInvitation>> _invitationRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly ActiveTenantInvitationsQueryHandler _handler;

    public ActiveTenantInvitationsQueryHandlerTests()
    {
        _currentUserMock.Setup(u => u.Email).Returns("user@test.com");

        _handler = new ActiveTenantInvitationsQueryHandler(
            _invitationRepoMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ActiveTenantInvitationsQuery ValidQuery() => new ActiveTenantInvitationsQuery();

    private static TenantInvitation BuildInvitation(string email) => new TenantInvitation
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        Tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test Tenant" },
        Email = email,
        Token = "token-abc",
        InvitedByUserId = Guid.NewGuid(),
        InvitedByUser = new User { Email = "inviter@test.com", FirstName = "Jan", LastName = "Kowalski" },
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        IsActive = true,
        Status = InvitationStatus.Pending
    };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoInvitations_ReturnsEmptyCollection()
    {
        // Arrange
        _invitationRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<TenantInvitation, bool>>>(),
                It.IsAny<Func<IQueryable<TenantInvitation>, IIncludableQueryable<TenantInvitation, object>>[]>()))
            .ReturnsAsync(new List<TenantInvitation>());

        ActiveTenantInvitationsQuery query = ValidQuery();

        // Act
        IEnumerable<TenantInvitationWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenInvitationsExist_ReturnsMappedInvitations()
    {
        // Arrange
        TenantInvitation invitation = BuildInvitation("user@test.com");

        _invitationRepoMock
            .Setup(r => r.GetBySearch(
                It.IsAny<Expression<Func<TenantInvitation, bool>>>(),
                It.IsAny<Func<IQueryable<TenantInvitation>, IIncludableQueryable<TenantInvitation, object>>[]>()))
            .ReturnsAsync(new List<TenantInvitation> { invitation });

        ActiveTenantInvitationsQuery query = ValidQuery();

        // Act
        IEnumerable<TenantInvitationWeb> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        TenantInvitationWeb dto = result.First();
        dto.InvitationId.Should().Be(invitation.Id);
        dto.TenantId.Should().Be(invitation.TenantId);
        dto.TenantName.Should().Be(invitation.Tenant.Name);
        dto.Token.Should().Be(invitation.Token);
    }
}
