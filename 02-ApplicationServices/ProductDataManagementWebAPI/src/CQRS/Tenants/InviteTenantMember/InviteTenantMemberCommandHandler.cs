using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;
using Microsoft.Extensions.Options;
using Business.Interfaces.Configurations;
using Business.Interfaces.Exceptions;
using Business.Interfaces.DTO;

namespace CQRS.Tenants.InviteTenantMember
{
    public class InviteTenantMemberCommandHandler : IRequestHandler<InviteTenantMemberCommand, Unit>
    {
        private readonly IRepository<TenantInvitation> invitationRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly ICurrentUser currentUser;
        private readonly IEmailSender emailSender;
        private readonly IOptions<FrontendSettings> frontendSettings;
        private readonly ITokenGenerator tokenGenerator;

        public InviteTenantMemberCommandHandler(
            IRepository<TenantInvitation> invitationRepo,
            IRepository<TenantMember> tenantMemberRepo,
            ICurrentUser currentUser,
            IEmailSender emailSender,
            IOptions<FrontendSettings> frontendSettings,
            ITokenGenerator tokenGenerator)
        {
            this.invitationRepo = invitationRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.currentUser = currentUser;
            this.emailSender = emailSender;
            this.frontendSettings = frontendSettings;
            this.tokenGenerator = tokenGenerator;
        }

        public async Task<Unit> Handle(InviteTenantMemberCommand request, CancellationToken cancellationToken)
        {
            TenantMember? membership = await tenantMemberRepo.GetFirstBySearch(m => m.TenantId == request.TenantId && m.UserId == currentUser.Id && m.IsActive);
            if (membership == null || membership.Role != Entities.Enums.TenantRole.Admin)
            {
                throw new ForbiddenApiException("Only tenant admins can invite members.");
            }

            string token = tokenGenerator.GenerateToken();
            TenantInvitation invitation = new TenantInvitation
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                Email = request.Email.Trim().ToLowerInvariant(),
                Token = token,
                CreatedAt = DateTime.UtcNow,
                InvitedByUserId = currentUser.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsActive = true,
                Status = InvitationStatus.Pending
            };

            await invitationRepo.Insert(invitation);

            string baseUrl = frontendSettings.Value.BaseUrl.TrimEnd('/');
            string path = frontendSettings.Value.InvitationAcceptPath.TrimStart('/');
            string acceptUrl = $"{baseUrl}/{path}?token={Uri.EscapeDataString(token)}";
            await emailSender.SendEmailAsync(new EmailMessageDto
            {
                To = request.Email,
                Subject = "Zaproszenie do tenanta",
                TextBody = $"Kliknij aby do³¹czyæ: {acceptUrl}",
                HtmlBody = $"<p>Kliknij aby do³¹czyæ: <a href=\"{acceptUrl}\">Do³¹cz</a></p>"
            }, cancellationToken);

            return Unit.Value;
        }
    }
}