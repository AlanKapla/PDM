using Business.Interfaces.Configurations;
using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Projects.InviteProjectMember;
using Entities.Enums;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Options;
using Moq;
using Repositories.Repository.Interfaces;
using System.Linq.Expressions;

namespace CQRS.Tests.Projects;

public sealed class InviteProjectMemberCommandHandlerTests
{
    private readonly Mock<IRepository<TenantInvitation>> _invitationRepoMock = new();
    private readonly Mock<IRepository<TenantInvitationModulePermission>> _modulePermissionRepoMock = new();
    private readonly Mock<IRepository<TenantMember>> _tenantMemberRepoMock = new();
    private readonly Mock<IReadRepository<User>> _userRepoMock = new();
    private readonly Mock<IReadRepository<Project>> _projectRepoMock = new();
    private readonly Mock<IReadRepository<Tenant>> _tenantRepoMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEmailSender> _emailSenderMock = new();
    private readonly Mock<INotificationSender> _notificationSenderMock = new();
    private readonly Mock<ITokenGenerator> _tokenGeneratorMock = new();
    private readonly Mock<IReadRepository<Notification>> _notificationRepoMock = new();
    private readonly Mock<IProjectMembershipProvisioner> _membershipProvisionerMock = new();
    private readonly IOptions<FrontendSettings> _frontendSettings;
    private readonly InviteProjectMemberCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();

    public InviteProjectMemberCommandHandlerTests()
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

        _modulePermissionRepoMock
            .Setup(r => r.Delete(It.IsAny<TenantInvitationModulePermission>()))
            .Returns(Task.CompletedTask);

        _modulePermissionRepoMock
            .Setup(r => r.Insert(It.IsAny<TenantInvitationModulePermission>()))
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

        _handler = new InviteProjectMemberCommandHandler(
            _invitationRepoMock.Object,
            _modulePermissionRepoMock.Object,
            _tenantMemberRepoMock.Object,
            _userRepoMock.Object,
            _projectRepoMock.Object,
            _tenantRepoMock.Object,
            _currentUserMock.Object,
            _emailSenderMock.Object,
            _notificationSenderMock.Object,
            _frontendSettings,
            _tokenGeneratorMock.Object,
            _notificationRepoMock.Object,
            _membershipProvisionerMock.Object);
    }

    private static InviteProjectMemberCommand ValidCommand(Guid tenantId, Guid projectId) =>
        new InviteProjectMemberCommand
        {
            TenantId = tenantId,
            ProjectId = projectId,
            Email = "invite@test.com",
            IsAdmin = false,
            Modules = new List<ProjectModule> { ProjectModule.Files }
        };

    private static Project BuildProject(Guid id, Guid tenantId) => new Project
    {
        Id = id,
        TenantId = tenantId,
        Name = "Test Project",
        IsActive = true
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

    private void SetupProjectAndTenant(Guid tenantId, Guid projectId)
    {
        _projectRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildProject(projectId, tenantId));

        _tenantRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Tenant, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildTenant(tenantId));
    }

    [Fact]
    public async Task Handle_WhenProjectNotFound_ThrowsNotFoundApiException()
    {
        _projectRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        InviteProjectMemberCommand command = ValidCommand(tenantId, projectId);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenTenantNotFound_ThrowsNotFoundApiException()
    {
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        _projectRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildProject(projectId, tenantId));

        _tenantRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<Tenant, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        InviteProjectMemberCommand command = ValidCommand(tenantId, projectId);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundApiException>();
    }

    [Fact]
    public async Task Handle_WhenUserIsActiveTenantMember_ProvisionsDirectlyWithoutCreatingInvitation()
    {
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        User existingUser = BuildUser("invite@test.com");

        SetupProjectAndTenant(tenantId, projectId);

        _userRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _tenantMemberRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<TenantMember, bool>>>()))
            .ReturnsAsync(new TenantMember
            {
                TenantId = tenantId,
                UserId = existingUser.Id,
                IsActive = true
            });

        InviteProjectMemberCommand command = ValidCommand(tenantId, projectId);

        Unit result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _membershipProvisionerMock.Verify(
            s => s.ProvisionProjectMemberAsync(
                tenantId,
                projectId,
                existingUser.Id,
                command.IsAdmin,
                command.Modules,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _invitationRepoMock.Verify(r => r.Insert(It.IsAny<TenantInvitation>()), Times.Never);
        _emailSenderMock.Verify(s => s.SendEmailAsync(It.IsAny<EmailMessageDto>(), It.IsAny<CancellationToken>()), Times.Never);
        _notificationSenderMock.Verify(s => s.EnqueueAsync(It.IsAny<NotificationPayloadDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPendingTenantOnlyInvitationExists_UpdatesProjectScopeAndExtendsExpiry()
    {
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        TenantInvitation existingInvitation = new TenantInvitation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProjectId = null,
            Email = "invite@test.com",
            Token = "existing-token",
            IsActive = true,
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            ModulePermissions = new List<TenantInvitationModulePermission>()
        };

        SetupProjectAndTenant(tenantId, projectId);

        _userRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _invitationRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantInvitation, bool>>>(),
                It.IsAny<Func<IQueryable<TenantInvitation>, IIncludableQueryable<TenantInvitation, object>>[]>()))
            .ReturnsAsync(existingInvitation);

        InviteProjectMemberCommand command = ValidCommand(tenantId, projectId);

        Unit result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        existingInvitation.ProjectId.Should().Be(projectId);
        existingInvitation.IsAdmin.Should().BeFalse();
        existingInvitation.InvitedByUserId.Should().Be(_userId);
        existingInvitation.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
        _invitationRepoMock.Verify(r => r.Insert(It.IsAny<TenantInvitation>()), Times.Never);
        _invitationRepoMock.Verify(r => r.Update(existingInvitation), Times.Once);
        _modulePermissionRepoMock.Verify(r => r.Insert(It.IsAny<TenantInvitationModulePermission>()), Times.Once);
        _emailSenderMock.Verify(s => s.SendEmailAsync(It.IsAny<EmailMessageDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPendingProjectInvitationExists_UpdatesAndExtendsWithoutInsert()
    {
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        TenantInvitation existingInvitation = new TenantInvitation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProjectId = projectId,
            Email = "invite@test.com",
            Token = "existing-token",
            IsActive = true,
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsAdmin = false,
            ModulePermissions = new List<TenantInvitationModulePermission>
            {
                new TenantInvitationModulePermission
                {
                    InvitationId = Guid.NewGuid(),
                    Module = ProjectModule.Costs
                }
            }
        };

        SetupProjectAndTenant(tenantId, projectId);

        _userRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _invitationRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantInvitation, bool>>>(),
                It.IsAny<Func<IQueryable<TenantInvitation>, IIncludableQueryable<TenantInvitation, object>>[]>()))
            .ReturnsAsync(existingInvitation);

        InviteProjectMemberCommand command = ValidCommand(tenantId, projectId) with
        {
            Modules = new List<ProjectModule> { ProjectModule.Files, ProjectModule.Schedule }
        };

        Unit result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        existingInvitation.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
        _invitationRepoMock.Verify(r => r.Insert(It.IsAny<TenantInvitation>()), Times.Never);
        _invitationRepoMock.Verify(r => r.Update(existingInvitation), Times.Once);
        _modulePermissionRepoMock.Verify(r => r.Delete(It.IsAny<TenantInvitationModulePermission>()), Times.Once);
        _modulePermissionRepoMock.Verify(r => r.Insert(It.IsAny<TenantInvitationModulePermission>()), Times.AtLeast(2));
        _emailSenderMock.Verify(s => s.SendEmailAsync(It.IsAny<EmailMessageDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoPendingInvitation_CreatesNewProjectInvitation()
    {
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        SetupProjectAndTenant(tenantId, projectId);

        _userRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _invitationRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantInvitation, bool>>>(),
                It.IsAny<Func<IQueryable<TenantInvitation>, IIncludableQueryable<TenantInvitation, object>>[]>()))
            .ReturnsAsync((TenantInvitation?)null);

        InviteProjectMemberCommand command = ValidCommand(tenantId, projectId);

        Unit result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _invitationRepoMock.Verify(
            r => r.Insert(It.Is<TenantInvitation>(i =>
                i.TenantId == tenantId
                && i.ProjectId == projectId
                && i.Email == "invite@test.com"
                && i.Token == "generated-token"
                && i.IsActive
                && i.Status == InvitationStatus.Pending)),
            Times.Once);
        _modulePermissionRepoMock.Verify(r => r.Insert(It.IsAny<TenantInvitationModulePermission>()), Times.Once);
        _emailSenderMock.Verify(s => s.SendEmailAsync(It.IsAny<EmailMessageDto>(), It.IsAny<CancellationToken>()), Times.Once);
        _notificationSenderMock.Verify(s => s.EnqueueAsync(It.IsAny<NotificationPayloadDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserExistsButNotTenantMember_CreatesInvitationAndSendsInAppNotification()
    {
        Guid tenantId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        User existingUser = BuildUser("invite@test.com");

        SetupProjectAndTenant(tenantId, projectId);

        _userRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _tenantMemberRepoMock
            .Setup(r => r.GetFirstBySearch(It.IsAny<Expression<Func<TenantMember, bool>>>()))
            .ReturnsAsync((TenantMember?)null);

        _invitationRepoMock
            .Setup(r => r.GetFirstBySearch(
                It.IsAny<Expression<Func<TenantInvitation, bool>>>(),
                It.IsAny<Func<IQueryable<TenantInvitation>, IIncludableQueryable<TenantInvitation, object>>[]>()))
            .ReturnsAsync((TenantInvitation?)null);

        InviteProjectMemberCommand command = ValidCommand(tenantId, projectId);

        Unit result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _invitationRepoMock.Verify(r => r.Insert(It.IsAny<TenantInvitation>()), Times.Once);
        _emailSenderMock.Verify(s => s.SendEmailAsync(It.IsAny<EmailMessageDto>(), It.IsAny<CancellationToken>()), Times.Once);
        _notificationSenderMock.Verify(s => s.EnqueueAsync(It.IsAny<NotificationPayloadDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
