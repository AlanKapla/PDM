using Business.Interfaces.Configurations;
using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Tenants.InviteTenantMember;
using Entities.Models.Notifications;
using Entities.Models.Tenants;
using Entities.Models.Users;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Options;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Tenants;

public sealed class InviteTenantMemberCommandHandlerTests
{
    private readonly Mock<IRepository<TenantInvitation>> _invitationRepoMock = new();
    private readonly Mock<IReadRepository<User>> _userRepoMock = new();
    private readonly Mock<IReadRepository<Tenant>> _tenantRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEmailSender> _emailSenderMock = new();
    private readonly Mock<INotificationSender> _notificationSenderMock = new();
    private readonly Mock<ITokenGenerator> _tokenGeneratorMock = new();
    private readonly Mock<IReadRepository<Notification>> _notificationRepoMock = new();
    private readonly IOptions<FrontendSettings> _frontendSettings;
    private readonly InviteTenantMemberCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public InviteTenantMemberCommandHandlerTests()
    {
        _currentUserMock.Setup(u => u.Id).Returns(_userId);

        _frontendSettings = Options.Create(new FrontendSettings
        {
            BaseUrl = "https://app.test",
            HomePath = "/home"
        });

        _tokenGeneratorMock
            .Setup(t => t.GenerateToken(It.IsAny<int>()))
            .Returns("generated-token");

        _notificationRepoMock
            .Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<Notification, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _notificationSenderMock
            .Setup(s => s.EnqueueAsync(It.IsAny<NotificationPayloadDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _emailSenderMock
            .Setup(s => s.SendEmailAsync(It.IsAny<EmailMessageDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new InviteTenantMemberCommandHandler(
            _invitationRepoMock.Object,
            _userRepoMock.Object,
            _tenantRepoMock.Object,
            _currentUserMock.Object,
            _emailSenderMock.Object,
            _notificationSenderMock.Object,
            _frontendSettings,
            _tokenGeneratorMock.Object,
            _notificationRepoMock.Object);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static InviteTenantMemberCommand ValidCommand(Guid tenantId) => new InviteTenantMemberCommand
    {
        TenantId = tenantId,
        Email = "invite@test.com"
    };

    private static Tenant BuildTenant(Guid id) => new Tenant
    {
        Id = id,
        Name = "Test Tenant",
        IsActive = true
    };

    private static User BuildUser(string email) => new User
    {
        Id = Guid.NewGuid(),
        Email = email,
        FirstName = "John",
        LastName = "Doe",
        IsActive = true,
        AzureAdB2CObjectId = "azure-oid"
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

        InviteTenantMemberCommand command = ValidCommand(Guid.NewGuid());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenUserExists_InsertsInvitationAndSendsNotification()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        Tenant tenant = BuildTenant(tenantId);
        User existingUser = BuildUser("invite@test.com");

        _tenantRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Tenant, bool>>>()))
            .ReturnsAsync(tenant);

        _userRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(existingUser);

        InviteTenantMemberCommand command = ValidCommand(tenantId);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _invitationRepoMock.Verify(r => r.Insert(It.IsAny<TenantInvitation>()), Times.Once);
        _notificationSenderMock.Verify(s => s.EnqueueAsync(It.IsAny<NotificationPayloadDto>(), It.IsAny<CancellationToken>()), Times.Once);
        _emailSenderMock.Verify(s => s.SendEmailAsync(It.IsAny<EmailMessageDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
