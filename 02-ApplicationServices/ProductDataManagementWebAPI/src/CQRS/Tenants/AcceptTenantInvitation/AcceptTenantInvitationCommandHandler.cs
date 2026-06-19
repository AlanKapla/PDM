using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Enums;
using Entities.Models.Tenants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.AcceptTenantInvitation
{
    public sealed class AcceptTenantInvitationCommandHandler : IRequestHandler<AcceptTenantInvitationCommand, Unit>
    {
        private readonly IRepository<TenantInvitation> invitationRepo;
        private readonly IProjectMembershipProvisioner membershipProvisioner;
        private readonly IPermissionsVersionService permissionsVersionService;
        private readonly ICurrentUser currentUser;

        public AcceptTenantInvitationCommandHandler(
            IRepository<TenantInvitation> invitationRepo,
            IProjectMembershipProvisioner membershipProvisioner,
            IPermissionsVersionService permissionsVersionService,
            ICurrentUser currentUser)
        {
            this.invitationRepo = invitationRepo;
            this.membershipProvisioner = membershipProvisioner;
            this.permissionsVersionService = permissionsVersionService;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(AcceptTenantInvitationCommand request, CancellationToken cancellationToken)
        {
            TenantInvitation invitation = await invitationRepo.GetFirstBySearch(
                i => i.Token == request.Token && i.IsActive,
                q => q.Include(x => x.ModulePermissions))
                ?? throw new NotFoundApiException("TenantInvitation", request.Token);

            await membershipProvisioner.EnsureTenantMemberAsync(
                invitation.TenantId,
                currentUser.Id,
                cancellationToken);

            if (invitation.ProjectId.HasValue)
            {
                List<ProjectModule> modules = invitation.ModulePermissions
                    .Select(p => p.Module)
                    .ToList();

                await membershipProvisioner.ProvisionProjectMemberAsync(
                    invitation.TenantId,
                    invitation.ProjectId.Value,
                    currentUser.Id,
                    invitation.IsAdmin,
                    modules,
                    cancellationToken);
            }
            else
            {
                await permissionsVersionService.BumpVersionAsync(currentUser.Id, cancellationToken);
            }

            invitation.Status = InvitationStatus.Accepted;
            invitation.AcceptedAt = DateTime.UtcNow;
            invitation.IsActive = false;
            await invitationRepo.Update(invitation);

            return Unit.Value;
        }
    }
}
