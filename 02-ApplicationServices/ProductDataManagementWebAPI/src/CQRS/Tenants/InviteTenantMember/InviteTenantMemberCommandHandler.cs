using Business.Implementation.Helpers;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using CQRS.Helpers;
using Entities.Models.Notifications;
using Entities.Models.Tenants;
using Entities.Models.Users;
using MediatR;
using Repositories.Repository.Interfaces;
using Microsoft.Extensions.Options;
using Business.Interfaces.Configurations;
using Business.Interfaces.DTO;
using DtoNotificationType = Business.Interfaces.DTO.NotificationType;
using Business.Interfaces.Model;

namespace CQRS.Tenants.InviteTenantMember
{
    public sealed class InviteTenantMemberCommandHandler : IRequestHandler<InviteTenantMemberCommand, Unit>
    {
        private readonly IRepository<TenantInvitation> invitationRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly IReadRepository<Tenant> tenantRepo;
        private readonly ICurrentUser currentUser;
        private readonly IEmailSender emailSender;
        private readonly INotificationSender notificationSender;
        private readonly IOptions<FrontendSettings> frontendSettings;
        private readonly ITokenGenerator tokenGenerator;
        private readonly IReadRepository<Notification> notificationRepo;

        public InviteTenantMemberCommandHandler(
            IRepository<TenantInvitation> invitationRepo,
            IRepository<TenantMember> tenantMemberRepo,
            IReadRepository<User> userRepo,
            IReadRepository<Tenant> tenantRepo,
            ICurrentUser currentUser,
            IEmailSender emailSender,
            INotificationSender notificationSender,
            IOptions<FrontendSettings> frontendSettings,
            ITokenGenerator tokenGenerator,
            IReadRepository<Notification> notificationRepo)
        {
            this.invitationRepo = invitationRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.userRepo = userRepo;
            this.tenantRepo = tenantRepo;
            this.currentUser = currentUser;
            this.emailSender = emailSender;
            this.notificationSender = notificationSender;
            this.frontendSettings = frontendSettings;
            this.tokenGenerator = tokenGenerator;
            this.notificationRepo = notificationRepo;
        }

        public async Task<Unit> Handle(InviteTenantMemberCommand request, CancellationToken cancellationToken)
        {
            Tenant tenant = await tenantRepo.GetFirstBySearch(t => t.Id == request.TenantId)
                ?? throw new NotFoundApiException(nameof(Tenant), request.TenantId.ToString());

            string normalizedEmail = request.Email.Trim().ToLowerInvariant();

            TenantInvitation? existingInvitation = await invitationRepo.GetFirstBySearch(
                i => i.TenantId == request.TenantId
                     && i.Email == normalizedEmail
                     && i.IsActive
                     && i.Status == InvitationStatus.Pending
                     && i.ExpiresAt > DateTime.UtcNow);

            if (existingInvitation is not null)
            {
                throw new ConflictApiException(
                    nameof(TenantInvitation),
                    normalizedEmail,
                    "Aktywne zaproszenie dla tego adresu email już istnieje.");
            }

            User? existingUser = await userRepo.GetFirstBySearch(u => u.Email == normalizedEmail && u.IsActive);

            if (existingUser is not null)
            {
                bool alreadyMember = await tenantMemberRepo.AnyAsync(
                    m => m.TenantId == request.TenantId
                         && m.UserId == existingUser.Id
                         && m.IsActive,
                    cancellationToken);

                if (alreadyMember)
                {
                    throw new ConflictApiException(
                        nameof(TenantMember),
                        normalizedEmail,
                        "Użytkownik jest już aktywnym członkiem tej organizacji.");
                }
            }

            string token = tokenGenerator.GenerateToken();
            TenantInvitation invitation = new TenantInvitation
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                Email = normalizedEmail,
                Token = token,
                CreatedAt = DateTime.UtcNow,
                InvitedByUserId = currentUser.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsActive = true,
                Status = InvitationStatus.Pending
            };

            await invitationRepo.Insert(invitation);

            string tenantName = tenant.Name;

            await SendInvitationEmailAsync(
                normalizedEmail,
                tenantName,
                existingUser is not null,
                cancellationToken);

            if (existingUser is not null)
            {
                await SendInAppNotificationAsync(
                    existingUser,
                    request.TenantId,
                    tenantName,
                    invitation.Id,
                    cancellationToken);
            }

            return Unit.Value;
        }

        private async Task SendInvitationEmailAsync(
            string email,
            string tenantName,
            bool userExistsInSystem,
            CancellationToken cancellationToken)
        {
            string baseUrl = frontendSettings.Value.BaseUrl.TrimEnd('/');
            string path = userExistsInSystem
                ? "tenants/invitations"
                : frontendSettings.Value.HomePath.TrimStart('/');
            string acceptUrl = $"{baseUrl}/{path}";

            string bodyText = userExistsInSystem
                ? $"Zostałeś zaproszony do organizacji <strong style=\"color:#1a1a1a;\">{tenantName}</strong> na platformie Brickly. Zaloguj się i zaakceptuj zaproszenie w aplikacji."
                : $"Zostałeś zaproszony do organizacji <strong style=\"color:#1a1a1a;\">{tenantName}</strong> na platformie Brickly. Utwórz konto, aby uzyskać dostęp i zacząć współpracę z zespołem.";

            string ctaLabel = userExistsInSystem ? "Zobacz zaproszenie" : "Utwórz konto i dołącz";

            string textBody = userExistsInSystem
                ? $"Zostałeś zaproszony do {tenantName}. Zaloguj się i zaakceptuj zaproszenie: {acceptUrl}"
                : $"Zostałeś zaproszony do {tenantName}. Aby zaakceptować zaproszenie, utwórz konto klikając w link: {acceptUrl}";

            string htmlBody = EmailTemplateLoader.Load("tenant-invitation.html", new Dictionary<string, string>
            {
                { "tenantName", tenantName },
                { "acceptUrl", acceptUrl },
                { "bodyText", bodyText },
                { "ctaLabel", ctaLabel }
            });

            await emailSender.SendEmailAsync(new EmailMessageDto
            {
                To = email,
                Subject = $"Zaproszenie do {tenantName}",
                TextBody = textBody,
                HtmlBody = htmlBody
            }, cancellationToken);
        }

        private async Task SendInAppNotificationAsync(
            User existingUser,
            Guid tenantId,
            string tenantName,
            Guid invitationId,
            CancellationToken cancellationToken)
        {
            NotificationDto notification = new NotificationDto
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ProjectId = null,
                UserId = existingUser.Id,
                AzureAdB2CObjectId = existingUser.AzureAdB2CObjectId,
                Type = DtoNotificationType.Info,
                Title = "Zaproszenie do organizacji",
                Message = $"Zostałeś zaproszony do {tenantName}",
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                Metadata = new Dictionary<string, object?>
                {
                    { "invitationId", invitationId },
                    { "tenantId", tenantId },
                    { "tenantName", tenantName }
                }
            };

            NotificationPayloadDto payload = await NotificationPayloadHelper.CreatePayloadAsync(
                notification,
                notificationRepo,
                cancellationToken);
            await notificationSender.EnqueueAsync(payload, cancellationToken);
        }
    }
}
