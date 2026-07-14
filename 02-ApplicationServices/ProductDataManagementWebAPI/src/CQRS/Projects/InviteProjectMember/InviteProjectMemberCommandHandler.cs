using Business.Implementation.Helpers;
using Business.Interfaces.Configurations;
using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Helpers;
using Entities.Enums;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Repositories.Repository.Interfaces;
using DtoNotificationType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.Projects.InviteProjectMember;

public sealed class InviteProjectMemberCommandHandler : IRequestHandler<InviteProjectMemberCommand, Unit>
{
    private readonly IRepository<TenantInvitation> invitationRepo;
    private readonly IRepository<TenantInvitationModulePermission> modulePermissionRepo;
    private readonly IRepository<TenantMember> tenantMemberRepo;
    private readonly IReadRepository<User> userRepo;
    private readonly IReadRepository<Project> projectRepo;
    private readonly IReadRepository<Tenant> tenantRepo;
    private readonly ICurrentUser currentUser;
    private readonly IEmailSender emailSender;
    private readonly INotificationSender notificationSender;
    private readonly IOptions<FrontendSettings> frontendSettings;
    private readonly ITokenGenerator tokenGenerator;
    private readonly IReadRepository<Notification> notificationRepo;
    private readonly IProjectMembershipProvisioner membershipProvisioner;

    public InviteProjectMemberCommandHandler(
        IRepository<TenantInvitation> invitationRepo,
        IRepository<TenantInvitationModulePermission> modulePermissionRepo,
        IRepository<TenantMember> tenantMemberRepo,
        IReadRepository<User> userRepo,
        IReadRepository<Project> projectRepo,
        IReadRepository<Tenant> tenantRepo,
        ICurrentUser currentUser,
        IEmailSender emailSender,
        INotificationSender notificationSender,
        IOptions<FrontendSettings> frontendSettings,
        ITokenGenerator tokenGenerator,
        IReadRepository<Notification> notificationRepo,
        IProjectMembershipProvisioner membershipProvisioner)
    {
        this.invitationRepo = invitationRepo;
        this.modulePermissionRepo = modulePermissionRepo;
        this.tenantMemberRepo = tenantMemberRepo;
        this.userRepo = userRepo;
        this.projectRepo = projectRepo;
        this.tenantRepo = tenantRepo;
        this.currentUser = currentUser;
        this.emailSender = emailSender;
        this.notificationSender = notificationSender;
        this.frontendSettings = frontendSettings;
        this.tokenGenerator = tokenGenerator;
        this.notificationRepo = notificationRepo;
        this.membershipProvisioner = membershipProvisioner;
    }

    public async Task<Unit> Handle(InviteProjectMemberCommand request, CancellationToken cancellationToken)
    {
        Project project = await projectRepo.GetFirstBySearch(
            p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
            cancellationToken)
            ?? throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());

        Tenant tenant = await tenantRepo.GetFirstBySearch(t => t.Id == request.TenantId, cancellationToken)
            ?? throw new NotFoundApiException(nameof(Tenant), request.TenantId.ToString());

        string normalizedEmail = request.Email.Trim().ToLowerInvariant();
        User? existingUser = await userRepo.GetFirstBySearch(
            u => u.Email == normalizedEmail && u.IsActive,
            cancellationToken);

        if (existingUser is not null)
        {
            TenantMember? tenantMember = await tenantMemberRepo.GetFirstBySearch(
                m => m.TenantId == request.TenantId
                    && m.UserId == existingUser.Id
                    && m.IsActive);

            if (tenantMember is not null)
            {
                await membershipProvisioner.ProvisionProjectMemberAsync(
                    request.TenantId,
                    request.ProjectId,
                    existingUser.Id,
                    request.IsAdmin,
                    request.Modules,
                    cancellationToken);

                await SendAddedToProjectNotificationAsync(
                    existingUser,
                    tenant,
                    project,
                    cancellationToken);

                return Unit.Value;
            }
        }

        TenantInvitation? existingInvitation = await invitationRepo.GetFirstBySearch(
            i => i.TenantId == request.TenantId
                && i.Email == normalizedEmail
                && i.IsActive
                && i.Status == InvitationStatus.Pending
                && i.ExpiresAt > DateTime.UtcNow,
            q => q.Include(x => x.ModulePermissions));

        if (existingInvitation is not null)
        {
            existingInvitation.ProjectId = request.ProjectId;
            existingInvitation.IsAdmin = request.IsAdmin;
            existingInvitation.InvitedByUserId = currentUser.Id;

            await InvitationHelper.ReplaceModulePermissionsAsync(
                modulePermissionRepo,
                existingInvitation,
                request.IsAdmin,
                request.Modules,
                cancellationToken);

            await InvitationHelper.ExtendPendingInvitationAsync(
                invitationRepo,
                existingInvitation,
                cancellationToken);

            await SendInvitationEmailAsync(
                normalizedEmail,
                tenant.Name,
                project.Name,
                existingUser is not null,
                existingInvitation.Token,
                cancellationToken);

            if (existingUser is not null)
            {
                await SendInAppInvitationNotificationAsync(
                    existingUser,
                    tenant,
                    project,
                    existingInvitation.Id,
                    cancellationToken);
            }

            return Unit.Value;
        }

        string token = tokenGenerator.GenerateToken();
        TenantInvitation invitation = new TenantInvitation
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            ProjectId = request.ProjectId,
            Email = normalizedEmail,
            Token = token,
            CreatedAt = DateTime.UtcNow,
            InvitedByUserId = currentUser.Id,
            ExpiresAt = InvitationHelper.NewExpiryUtc(),
            IsActive = true,
            Status = InvitationStatus.Pending,
            IsAdmin = request.IsAdmin
        };

        await invitationRepo.Insert(invitation);

        await InvitationHelper.ReplaceModulePermissionsAsync(
            modulePermissionRepo,
            invitation,
            request.IsAdmin,
            request.Modules,
            cancellationToken);

        await SendInvitationEmailAsync(
            normalizedEmail,
            tenant.Name,
            project.Name,
            existingUser is not null,
            token,
            cancellationToken);

        if (existingUser is not null)
        {
            await SendInAppInvitationNotificationAsync(
                existingUser,
                tenant,
                project,
                invitation.Id,
                cancellationToken);
        }

        return Unit.Value;
    }

    private async Task SendInvitationEmailAsync(
        string email,
        string tenantName,
        string projectName,
        bool userExistsInSystem,
        string token,
        CancellationToken cancellationToken)
    {
        string baseUrl = frontendSettings.Value.BaseUrl.TrimEnd('/');
        string acceptUrl = $"{baseUrl}/invitations/accept?token={Uri.EscapeDataString(token)}&type=project";

        string bodyText = userExistsInSystem
            ? $"Zostałeś zaproszony do projektu <strong style=\"color:#1a1a1a;\">{projectName}</strong> w organizacji <strong style=\"color:#1a1a1a;\">{tenantName}</strong> na platformie Brickly. Zaloguj się i zaakceptuj zaproszenie w aplikacji."
            : $"Zostałeś zaproszony do projektu <strong style=\"color:#1a1a1a;\">{projectName}</strong> w organizacji <strong style=\"color:#1a1a1a;\">{tenantName}</strong> na platformie Brickly. Utwórz konto, aby uzyskać dostęp i zacząć współpracę z zespołem.";

        string ctaLabel = userExistsInSystem ? "Zobacz zaproszenie" : "Utwórz konto i dołącz";

        string textBody = userExistsInSystem
            ? $"Zostałeś zaproszony do projektu {projectName} w organizacji {tenantName}. Zaloguj się i zaakceptuj zaproszenie: {acceptUrl}"
            : $"Zostałeś zaproszony do projektu {projectName} w organizacji {tenantName}. Aby zaakceptować zaproszenie, utwórz konto: {acceptUrl}";

        string htmlBody = EmailTemplateLoader.Load("project-invitation.html", new Dictionary<string, string>
        {
            { "tenantName", tenantName },
            { "projectName", projectName },
            { "acceptUrl", acceptUrl },
            { "bodyText", bodyText },
            { "ctaLabel", ctaLabel }
        });

        await emailSender.SendEmailAsync(new EmailMessageDto
        {
            To = email,
            Subject = $"Zaproszenie do projektu {projectName}",
            TextBody = textBody,
            HtmlBody = htmlBody
        }, cancellationToken);
    }

    private async Task SendInAppInvitationNotificationAsync(
        User existingUser,
        Tenant tenant,
        Project project,
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        NotificationDto notification = new NotificationDto
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            ProjectId = project.Id,
            UserId = existingUser.Id,
            AzureAdB2CObjectId = existingUser.AzureAdB2CObjectId,
            Type = DtoNotificationType.Info,
            Title = "Zaproszenie do projektu",
            Message = $"Zostałeś zaproszony do projektu {project.Name}",
            CreatedAt = DateTime.UtcNow,
            IsRead = false,
            Metadata = new Dictionary<string, object?>
            {
                { "invitationId", invitationId },
                { "tenantId", tenant.Id },
                { "tenantName", tenant.Name },
                { "projectId", project.Id },
                { "projectName", project.Name },
                { "invitationType", "project" }
            }
        };

        NotificationPayloadDto payload = await NotificationPayloadHelper.CreatePayloadAsync(
            notification,
            notificationRepo,
            cancellationToken);
        await notificationSender.EnqueueAsync(payload, cancellationToken);
    }

    private async Task SendAddedToProjectNotificationAsync(
        User existingUser,
        Tenant tenant,
        Project project,
        CancellationToken cancellationToken)
    {
        NotificationDto notification = new NotificationDto
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            ProjectId = project.Id,
            UserId = existingUser.Id,
            AzureAdB2CObjectId = existingUser.AzureAdB2CObjectId,
            Type = DtoNotificationType.Info,
            Title = "Dodano do projektu",
            Message = $"Zostałeś dodany do projektu: {project.Name}",
            CreatedAt = DateTime.UtcNow,
            IsRead = false,
            Metadata = new Dictionary<string, object?>
            {
                { "projectId", project.Id },
                { "projectName", project.Name },
                { "addedByUserId", currentUser.Id }
            }
        };

        NotificationPayloadDto payload = await NotificationPayloadHelper.CreatePayloadAsync(
            notification,
            notificationRepo,
            cancellationToken);
        await notificationSender.EnqueueAsync(payload, cancellationToken);
    }
}
