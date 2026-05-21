using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Enums;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.AcceptTenantInvitation
{
    public sealed class AcceptTenantInvitationCommandHandler : IRequestHandler<AcceptTenantInvitationCommand, Unit>
    {
        private readonly IRepository<TenantInvitation> invitationRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly IReadRepository<Role> roleRepo;
        private readonly IPermissionsVersionService permissionsVersionService;
        private readonly ICurrentUser currentUser;

        public AcceptTenantInvitationCommandHandler(
            IRepository<TenantInvitation> invitationRepo,
            IRepository<TenantMember> tenantMemberRepo,
            IReadRepository<Role> roleRepo,
            IPermissionsVersionService permissionsVersionService,
            ICurrentUser currentUser)
        {
            this.invitationRepo = invitationRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.roleRepo = roleRepo;
            this.permissionsVersionService = permissionsVersionService;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(AcceptTenantInvitationCommand request, CancellationToken cancellationToken)
        {
            TenantInvitation invitation = await invitationRepo.GetFirstBySearch(i => i.Token == request.Token && i.IsActive)
                ?? throw new NotFoundApiException("TenantInvitation", request.Token);

            Role? memberRole = await roleRepo.GetFirstBySearch(
                r => r.Scope == RoleScope.Tenant && r.Code == RoleCodes.TenantMember,
                cancellationToken);

            if (memberRole is null)
            {
                throw new NotFoundApiException(nameof(Role), RoleCodes.TenantMember);
            }

            TenantMember? existing = await tenantMemberRepo.GetFirstBySearch(m => m.TenantId == invitation.TenantId && m.UserId == currentUser.Id);
            if (existing is null)
            {
                TenantMember member = new TenantMember
                {
                    TenantId = invitation.TenantId,
                    UserId = currentUser.Id,
                    RoleId = memberRole.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await tenantMemberRepo.Insert(member);
            }
            else if (!existing.IsActive)
            {
                existing.IsActive = true;
                existing.RoleId = memberRole.Id;
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
