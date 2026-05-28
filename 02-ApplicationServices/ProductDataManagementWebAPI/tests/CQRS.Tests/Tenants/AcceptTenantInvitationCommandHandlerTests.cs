using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Tenants.AcceptTenantInvitation;
using Entities.Enums;
using Entities.Models.Tenants;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Tenants;

public sealed class AcceptTenantInvitationCommandHandlerTests
{
    private readonly Mock<IRepository<TenantInvitation>> _invitationRepoMock = new();
    private readonly Mock<IRepository<TenantMember>> _tenantMemberRepoMock = new();
    private readonly Mock<IPermissionsVersionService> _permissionsVersionServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly AcceptTenantInvitationCommandHandler _handler;

    public AcceptTenantInvitationCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _permissionsVersionServiceMock
            .Setup(s => s.BumpVersionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new AcceptTenantInvitationCommandHandler(
            _invitationRepoMock.Object,
            _tenantMemberRepoMock.Object,
            _permissionsVersionServiceMock.Object,
            _currentUserMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static AcceptTenantInvitationCommand ValidCommand() => new AcceptTenantInvitationCommand
    {
        Token = "valid-token"
    };

    private static TenantInvitation BuildInvitation(string token) => new TenantInvitation
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        Token = token,
        IsActive = true,
        Status = InvitationStatus.Pending,
        Email = "user@test.com",
        InvitedByUserId = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };

    // ─── Handle ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenInvitationNotFound_ThrowsNotFoundApiException()
    {
        // Arrange
        _invitationRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantInvitation, bool>>>(),
                It.IsAny<Func<IQueryable<TenantInvitation>, IIncludableQueryable<TenantInvitation, object>>[]>()))
            .ReturnsAsync((TenantInvitation?)null);

        AcceptTenantInvitationCommand command = ValidCommand();

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenMemberDoesNotExist_InsertsNewMemberAndReturnsUnit()
    {
        // Arrange
        TenantInvitation invitation = BuildInvitation("valid-token");

        _invitationRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantInvitation, bool>>>(),
                It.IsAny<Func<IQueryable<TenantInvitation>, IIncludableQueryable<TenantInvitation, object>>[]>()))
            .ReturnsAsync(invitation);

        _tenantMemberRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantMember, bool>>>(),
                It.IsAny<Func<IQueryable<TenantMember>, IIncludableQueryable<TenantMember, object>>[]>()))
            .ReturnsAsync((TenantMember?)null);

        AcceptTenantInvitationCommand command = ValidCommand();

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _tenantMemberRepoMock.Verify(r => r.Insert(It.IsAny<TenantMember>()), Times.Once);
        _invitationRepoMock.Verify(r => r.Update(It.Is<TenantInvitation>(i => i.Status == InvitationStatus.Accepted && !i.IsActive)), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMemberIsInactive_ReactivatesMemberAndReturnsUnit()
    {
        // Arrange
        TenantInvitation invitation = BuildInvitation("valid-token");
        TenantMember inactiveMember = new TenantMember
        {
            TenantId = invitation.TenantId,
            UserId = _currentUserMock.Object.Id,
            IsActive = false
        };

        _invitationRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantInvitation, bool>>>(),
                It.IsAny<Func<IQueryable<TenantInvitation>, IIncludableQueryable<TenantInvitation, object>>[]>()))
            .ReturnsAsync(invitation);

        _tenantMemberRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantMember, bool>>>(),
                It.IsAny<Func<IQueryable<TenantMember>, IIncludableQueryable<TenantMember, object>>[]>()))
            .ReturnsAsync(inactiveMember);

        AcceptTenantInvitationCommand command = ValidCommand();

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        inactiveMember.IsActive.Should().BeTrue();
        _tenantMemberRepoMock.Verify(r => r.Update(inactiveMember), Times.Once);
        _tenantMemberRepoMock.Verify(r => r.Insert(It.IsAny<TenantMember>()), Times.Never);
    }
}

