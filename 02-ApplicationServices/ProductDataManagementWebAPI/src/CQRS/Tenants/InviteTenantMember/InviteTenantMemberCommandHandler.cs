using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using CQRS.Helpers;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;
using Repositiories.Repository.Interfaces;
using Microsoft.Extensions.Options;
using Business.Interfaces.Configurations;
using Business.Interfaces.DTO;
using DtoNotificationType = Business.Interfaces.DTO.NotificationType;
using Business.Interfaces.Model;

namespace CQRS.Tenants.InviteTenantMember
{
    public class InviteTenantMemberCommandHandler : IRequestHandler<InviteTenantMemberCommand, Unit>
    {
        private readonly IRepository<TenantInvitation> invitationRepo;
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

            User? existingUser = await userRepo.GetFirstBySearch(u => u.Email == normalizedEmail && u.IsActive);

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

            if (existingUser == null)
            {
                string baseUrl = frontendSettings.Value.BaseUrl.TrimEnd('/');
                string path = frontendSettings.Value.HomePath.TrimStart('/');
                string acceptUrl = $"{baseUrl}/{path}";

                await emailSender.SendEmailAsync(new EmailMessageDto
                {
                    To = normalizedEmail,
                    Subject = $"Zaproszenie do {tenantName}",
                    TextBody = $"Zostałeś zaproszony do {tenantName}. Aby zaakceptować zaproszenie, utwórz konto klikając w link: {acceptUrl}",
                    HtmlBody = $@"
                        <p>Zostałeś zaproszony do <strong>{tenantName}</strong>.</p>
                        <p>Aby zaakceptować zaproszenie, utwórz konto klikając w poniższy link:</p>
                        <p><a href=""{acceptUrl}"">Utwórz konto i dołącz</a></p>
                        <p>To zaproszenie wygaśnie za 7 dni.</p>"
                }, cancellationToken);
            }
            else
            {
                var notification = new NotificationDto
                {
                    Id = Guid.NewGuid(),
                    TenantId = request.TenantId,
                    ProjectId = null,
                    UserId = existingUser.Id,
                    AzureAdB2CObjectId = existingUser.AzureAdB2CObjectId,
                    Type = DtoNotificationType.Info,
                    Title = "Zaproszenie do organizacji",
                    Message = $"Zostałeś zaproszony do {tenantName}",
                    CreatedAt = DateTimeOffset.UtcNow,
                    Readed = false,
                    Metadata = new Dictionary<string, object?>
                    {
                        { "invitationId", invitation.Id },
                        { "tenantId", request.TenantId },
                        { "tenantName", tenantName }
                    }
                };
                
                var payload = await NotificationPayloadHelper.CreatePayloadAsync(notification, notificationRepo, cancellationToken);
                await notificationSender.EnqueueAsync(payload, cancellationToken);
            }

            return Unit.Value;
        }
    }
}
