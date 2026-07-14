using Business.Interfaces.WebModels.Projects;
using Entities.Models.Tenants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.GetProjectInvitations;

public sealed class GetProjectInvitationsQueryHandler
    : IRequestHandler<GetProjectInvitationsQuery, IEnumerable<ProjectInvitationWeb>>
{
    private readonly IReadRepository<TenantInvitation> invitationRepo;

    public GetProjectInvitationsQueryHandler(IReadRepository<TenantInvitation> invitationRepo)
    {
        this.invitationRepo = invitationRepo;
    }

    public async Task<IEnumerable<ProjectInvitationWeb>> Handle(
        GetProjectInvitationsQuery request,
        CancellationToken cancellationToken)
    {
        IEnumerable<TenantInvitation> invites = await invitationRepo.GetBySearch(
            i => i.TenantId == request.TenantId
                && i.ProjectId == request.ProjectId
                && i.IsActive
                && i.Status == InvitationStatus.Pending
                && i.ExpiresAt > DateTime.UtcNow,
            q => q.Include(x => x.InvitedByUser)
                .Include(x => x.Tenant)
                .Include(x => x.Project)
                .Include(x => x.ModulePermissions));

        return invites.Select(MapToWeb).ToList();
    }

    private static ProjectInvitationWeb MapToWeb(TenantInvitation invitation) =>
        new()
        {
            InvitationId = invitation.Id,
            TenantId = invitation.TenantId,
            TenantName = invitation.Tenant?.Name ?? string.Empty,
            ProjectId = invitation.ProjectId!.Value,
            ProjectName = invitation.Project?.Name ?? string.Empty,
            Email = invitation.Email,
            IsAdmin = invitation.IsAdmin,
            Modules = invitation.ModulePermissions.Select(p => p.Module).ToList(),
            InvitedByUserEmail = invitation.InvitedByUser?.Email ?? string.Empty,
            InvitedByUserName = invitation.InvitedByUser is null
                ? string.Empty
                : $"{invitation.InvitedByUser.FirstName} {invitation.InvitedByUser.LastName}",
            CreatedAt = invitation.CreatedAt,
            ExpiresAt = invitation.ExpiresAt,
            Status = invitation.Status,
            Token = invitation.Token
        };
}
