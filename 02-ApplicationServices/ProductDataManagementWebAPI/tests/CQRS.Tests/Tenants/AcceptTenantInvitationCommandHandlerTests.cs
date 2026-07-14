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
    private readonly Mock<IProjectMembershipProvisioner> _membershipProvisionerMock = new();
    private readonly Mock<IPermissionsVersionService> _permissionsVersionServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly AcceptTenantInvitationCommandHandler _handler;

    public AcceptTenantInvitationCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _permissionsVersionServiceMock
            .Setup(s => s.BumpVersionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _membershipProvisionerMock
            .Setup(s => s.EnsureTenantMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _membershipProvisionerMock
            .Setup(s => s.ProvisionProjectMemberAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                It.IsAny<IReadOnlyList<ProjectModule>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new AcceptTenantInvitationCommandHandler(
            _invitationRepoMock.Object,
            _membershipProvisionerMock.Object,
            _permissionsVersionServiceMock.Object,
            _currentUserMock.Object);
    }

    private static AcceptTenantInvitationCommand ValidCommand() => new AcceptTenantInvitationCommand
    {
        Token = "valid-token"
    };

    private static TenantInvitation BuildInvitation(string token, Guid? projectId = null) => new TenantInvitation
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        ProjectId = projectId,
        Token = token,
        IsActive = true,
        Status = InvitationStatus.Pending,
        Email = "user@test.com",
        InvitedByUserId = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };

    [Fact]
    public async Task Handle_WhenInvitationNotFound_ThrowsNotFoundApiException()
    {
        _invitationRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantInvitation, bool>>>(),
                It.IsAny<Func<IQueryable<TenantInvitation>, IIncludableQueryable<TenantInvitation, object>>[]>()))
            .ReturnsAsync((TenantInvitation?)null);

        AcceptTenantInvitationCommand command = ValidCommand();

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenTenantOnlyInvitation_EnsuresTenantMemberAndReturnsUnit()
    {
        TenantInvitation invitation = BuildInvitation("valid-token");

        _invitationRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantInvitation, bool>>>(),
                It.IsAny<Func<IQueryable<TenantInvitation>, IIncludableQueryable<TenantInvitation, object>>[]>()))
            .ReturnsAsync(invitation);

        AcceptTenantInvitationCommand command = ValidCommand();

        Unit result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _membershipProvisionerMock.Verify(
            s => s.EnsureTenantMemberAsync(invitation.TenantId, _currentUserMock.Object.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        _membershipProvisionerMock.Verify(
            s => s.ProvisionProjectMemberAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                It.IsAny<IReadOnlyList<ProjectModule>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _permissionsVersionServiceMock.Verify(
            s => s.BumpVersionAsync(_currentUserMock.Object.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        _invitationRepoMock.Verify(r => r.Update(It.Is<TenantInvitation>(i => i.Status == InvitationStatus.Accepted && !i.IsActive)), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProjectInvitation_ProvisionsTenantAndProjectMember()
    {
        Guid projectId = Guid.NewGuid();
        TenantInvitation invitation = BuildInvitation("valid-token", projectId);

        _invitationRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantInvitation, bool>>>(),
                It.IsAny<Func<IQueryable<TenantInvitation>, IIncludableQueryable<TenantInvitation, object>>[]>()))
            .ReturnsAsync(invitation);

        AcceptTenantInvitationCommand command = ValidCommand();

        Unit result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _membershipProvisionerMock.Verify(
            s => s.EnsureTenantMemberAsync(invitation.TenantId, _currentUserMock.Object.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        _membershipProvisionerMock.Verify(
            s => s.ProvisionProjectMemberAsync(
                invitation.TenantId,
                projectId,
                _currentUserMock.Object.Id,
                invitation.IsAdmin,
                It.IsAny<IReadOnlyList<ProjectModule>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
