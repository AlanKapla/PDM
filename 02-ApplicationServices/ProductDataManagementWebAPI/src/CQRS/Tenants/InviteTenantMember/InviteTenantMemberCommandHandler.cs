using Business.Interfaces.Services;
using Business.Interfaces.Model;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;
using Repositiories.Repository.Interfaces;
using Microsoft.Extensions.Options;
using Business.Interfaces.Configurations;
using Business.Interfaces.DTO;
using DtoNotificationType = Business.Interfaces.DTO.NotificationType;
using Business.Interfaces.Exceptions;

namespace CQRS.Tenants.InviteTenantMember
{
    public class InviteTenantMemberCommandHandler : IRequestHandler<InviteTenantMemberCommand, Unit>
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

        public InviteTenantMemberCommandHandler(
            IRepository<TenantInvitation> invitationRepo,
            IRepository<TenantMember> tenantMemberRepo,
            IReadRepository<User> userRepo,
            IReadRepository<Tenant> tenantRepo,
            ICurrentUser currentUser,
            IEmailSender emailSender,
            INotificationSender notificationSender,
            IOptions<FrontendSettings> frontendSettings,
            ITokenGenerator tokenGenerator)
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
        }

        public async Task<Unit> Handle(InviteTenantMemberCommand request, CancellationToken cancellationToken)
        {
            // Wszystkie walidacje są wykonane w validatorze
            string normalizedEmail = request.Email.Trim().ToLowerInvariant();

            // Sprawdź czy użytkownik już istnieje w systemie
            User? existingUser = await userRepo.GetFirstBySearch(u => u.Email == normalizedEmail && u.IsActive);

            // Utwórz nowe zaproszenie
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

            Tenant tenant = await tenantRepo.GetFirstBySearch(t => t.Id == request.TenantId && t.IsActive)
                ?? throw new NotFoundApiException(nameof(Tenant), request.TenantId.ToString());

            string tenantName = tenant.Name;

            // Jeśli użytkownik NIE ISTNIEJE w systemie - wyślij email z instrukcją rejestracji
            if (existingUser == null)
            {
                string baseUrl = frontendSettings.Value.BaseUrl.TrimEnd('/');
                string path = frontendSettings.Value.HomePath.TrimStart('/');
                string acceptUrl = $"{baseUrl}/{path}";

                await emailSender.SendEmailAsync(new EmailMessageDto
                {
                    To = normalizedEmail,
                    Subject = $"Invitation to join {tenantName}",
                    TextBody = $"You have been invited to join {tenantName}. To accept this invitation, please create an account by clicking the link: {acceptUrl}",
                    HtmlBody = $@"
                        <p>You have been invited to join <strong>{tenantName}</strong>.</p>
                        <p>To accept this invitation, please create an account by clicking the link below:</p>
                        <p><a href=""{acceptUrl}"">Create Account and Join</a></p>
                        <p>This invitation will expire in 7 days.</p>"
                }, cancellationToken);
            }
            // Jeśli użytkownik ISTNIEJE - wyślij tylko notyfikację
            else
            {
                await notificationSender.EnqueueAsync(new NotificationDto
                {
                    Id = Guid.NewGuid(),
                    TenantId = request.TenantId,
                    ProjectId = null,
                    UserId = existingUser.Id,
                    Type = DtoNotificationType.Info,
                    Title = "Tenant Invitation",
                    Message = $"You have been invited to join {tenantName}",
                    CreatedAt = DateTimeOffset.UtcNow,
                    Readed = false,
                    Metadata = new Dictionary<string, object?>
                    {
                        { "invitationId", invitation.Id },
                        { "tenantId", request.TenantId },
                        { "tenantName", tenantName }
                    }
                }, cancellationToken);
            }

            return Unit.Value;
        }
    }
}
