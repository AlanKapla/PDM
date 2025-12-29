using Business.Interfaces.Model;
using Entities.Enums;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;
using Business.Interfaces.Exceptions;

namespace CQRS.Tenants.AcceptTenantInvitation
{
    public class AcceptTenantInvitationCommandHandler : IRequestHandler<AcceptTenantInvitationCommand, Unit>
    {
        private readonly IRepository<TenantInvitation> invitationRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly ICurrentUser currentUser;

        public AcceptTenantInvitationCommandHandler(
            IRepository<TenantInvitation> invitationRepo,
            IRepository<TenantMember> tenantMemberRepo,
            ICurrentUser currentUser)
        {
            this.invitationRepo = invitationRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(AcceptTenantInvitationCommand request, CancellationToken cancellationToken)
        {
            TenantInvitation? invitation = await invitationRepo.GetFirstBySearch(i => i.Token == request.Token && i.IsActive)
                ?? throw new NotFoundApiException("TenantInvitation", request.Token);

            // Create membership as Member
            TenantMember? existing = await tenantMemberRepo.GetFirstBySearch(m => m.TenantId == invitation.TenantId && m.UserId == currentUser.Id);
            if (existing == null)
            {
                TenantMember member = new TenantMember
                {
                    TenantId = invitation.TenantId,
                    UserId = currentUser.Id,
                    Role = TenantRole.Member,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await tenantMemberRepo.Insert(member);
            }
            else if (!existing.IsActive)
            {
                existing.IsActive = true;
                existing.Role = TenantRole.Member;
                await tenantMemberRepo.Update(existing);
            }

            invitation.Status = InvitationStatus.Accepted;
            invitation.AcceptedAt = DateTime.UtcNow;
            invitation.IsActive = false;
            await invitationRepo.Update(invitation);

            return Unit.Value;
        }
    }
}
