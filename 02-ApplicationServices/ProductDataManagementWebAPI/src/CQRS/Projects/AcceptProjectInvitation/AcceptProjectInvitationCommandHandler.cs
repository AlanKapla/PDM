using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Enums;
using Entities.Models.Tenants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.AcceptProjectInvitation;

public sealed class AcceptProjectInvitationCommandHandler : IRequestHandler<AcceptProjectInvitationCommand, Unit>
{
    private readonly IRepository<TenantInvitation> invitationRepo;
    private readonly IProjectMembershipProvisioner membershipProvisioner;
    private readonly ICurrentUser currentUser;

    public AcceptProjectInvitationCommandHandler(
        IRepository<TenantInvitation> invitationRepo,
        IProjectMembershipProvisioner membershipProvisioner,
        ICurrentUser currentUser)
    {
        this.invitationRepo = invitationRepo;
        this.membershipProvisioner = membershipProvisioner;
        this.currentUser = currentUser;
    }

    public async Task<Unit> Handle(AcceptProjectInvitationCommand request, CancellationToken cancellationToken)
    {
        TenantInvitation invitation = await invitationRepo.GetFirstBySearch(
            i => i.Token == request.Token
                && i.IsActive
                && i.ProjectId != null,
            q => q.Include(x => x.ModulePermissions))
            ?? throw new NotFoundApiException(nameof(TenantInvitation), request.Token);

        await membershipProvisioner.EnsureTenantMemberAsync(
            invitation.TenantId,
            currentUser.Id,
            cancellationToken);

        List<ProjectModule> modules = invitation.ModulePermissions
            .Select(p => p.Module)
            .ToList();

        await membershipProvisioner.ProvisionProjectMemberAsync(
            invitation.TenantId,
            invitation.ProjectId!.Value,
            currentUser.Id,
            invitation.IsAdmin,
            modules,
            cancellationToken);

        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptedAt = DateTime.UtcNow;
        invitation.IsActive = false;
        await invitationRepo.Update(invitation);

        return Unit.Value;
    }
}
