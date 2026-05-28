using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.Tenants;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.AcceptTenantInvitation
{
    public sealed class AcceptTenantInvitationCommandHandler : IRequestHandler<AcceptTenantInvitationCommand, Unit>
    {
        private readonly IRepository<TenantInvitation> invitationRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly IPermissionsVersionService permissionsVersionService;
        private readonly ICurrentUser currentUser;

        public AcceptTenantInvitationCommandHandler(
            IRepository<TenantInvitation> invitationRepo,
            IRepository<TenantMember> tenantMemberRepo,
            IPermissionsVersionService permissionsVersionService,
            ICurrentUser currentUser)
        {
            this.invitationRepo = invitationRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.permissionsVersionService = permissionsVersionService;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(AcceptTenantInvitationCommand request, CancellationToken cancellationToken)
        {
            TenantInvitation invitation = await invitationRepo.GetFirstBySearch(i => i.Token == request.Token && i.IsActive)
                ?? throw new NotFoundApiException("TenantInvitation", request.Token);

            TenantMember? existing = await tenantMemberRepo.GetFirstBySearch(m => m.TenantId == invitation.TenantId && m.UserId == currentUser.Id);
            if (existing is null)
            {
                TenantMember member = new TenantMember
                {
                    TenantId = invitation.TenantId,
                    UserId = currentUser.Id,
                    IsAdmin = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await tenantMemberRepo.Insert(member);
            }
            else if (!existing.IsActive)
            {
                existing.IsActive = true;
                existing.IsAdmin = false;
                await tenantMemberRepo.Update(existing);
            }

            await permissionsVersionService.BumpVersionAsync(currentUser.Id, cancellationToken);

            invitation.Status = InvitationStatus.Accepted;
            invitation.AcceptedAt = DateTime.UtcNow;
            invitation.IsActive = false;
            await invitationRepo.Update(invitation);

            return Unit.Value;
        }
    }
}
