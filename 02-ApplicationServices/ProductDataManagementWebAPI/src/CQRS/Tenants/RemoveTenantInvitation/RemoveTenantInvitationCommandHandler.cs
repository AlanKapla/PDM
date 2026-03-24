using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.RemoveTenantInvitation
{
    public class RemoveTenantInvitationCommandHandler : IRequestHandler<RemoveTenantInvitationCommand, Unit>
    {
        private readonly IReadRepository<Tenant> tenantRepo;
        private readonly IRepository<TenantInvitation> invitationRepo;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<RemoveTenantInvitationCommandHandler> logger;

        public RemoveTenantInvitationCommandHandler(
            IReadRepository<Tenant> tenantRepo,
            IRepository<TenantInvitation> invitationRepo,
            ICurrentUser currentUser,
            ILogger<RemoveTenantInvitationCommandHandler> logger)
        {
            this.tenantRepo = tenantRepo;
            this.invitationRepo = invitationRepo;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(RemoveTenantInvitationCommand request, CancellationToken cancellationToken)
        {
            Tenant tenant = (await tenantRepo.GetFirstBySearch(t => t.Id == request.TenantId))
                ?? throw new NotFoundApiException(nameof(Tenant), request.TenantId.ToString());

            TenantInvitation invitation = (await invitationRepo.GetFirstBySearch(
                i => i.Id == request.InvitationId && i.TenantId == request.TenantId))
                ?? throw new NotFoundApiException(nameof(TenantInvitation), request.InvitationId.ToString());

            invitation.IsActive = false;
            invitation.Status = InvitationStatus.Revoked;

            await invitationRepo.Update(invitation);

            logger.LogInformation(
                "Invitation {InvitationId} for tenant {TenantId} revoked by user {UserId}",
                request.InvitationId,
                request.TenantId,
                currentUser.Id);

            return Unit.Value;
        }
    }
}
