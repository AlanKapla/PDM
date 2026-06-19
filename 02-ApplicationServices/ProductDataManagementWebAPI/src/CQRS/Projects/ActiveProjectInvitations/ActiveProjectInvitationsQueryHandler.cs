using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;
using Entities.Models.Tenants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.ActiveProjectInvitations;

public sealed class ActiveProjectInvitationsQueryHandler
    : IRequestHandler<ActiveProjectInvitationsQuery, IEnumerable<ProjectInvitationWeb>>
{
    private readonly IReadRepository<TenantInvitation> invitationRepo;
    private readonly ICurrentUser currentUser;

    public ActiveProjectInvitationsQueryHandler(
        IReadRepository<TenantInvitation> invitationRepo,
        ICurrentUser currentUser)
    {
        this.invitationRepo = invitationRepo;
        this.currentUser = currentUser;
    }

    public async Task<IEnumerable<ProjectInvitationWeb>> Handle(
        ActiveProjectInvitationsQuery request,
        CancellationToken cancellationToken)
    {
        string email = currentUser.Email.Trim().ToLowerInvariant();
        IEnumerable<TenantInvitation> invites = await invitationRepo.GetBySearch(
            i => i.IsActive
                && i.Email.ToLower() == email
                && i.ProjectId != null
                && i.Status == InvitationStatus.Pending
                && i.ExpiresAt > DateTime.UtcNow,
            q => q.Include(x => x.InvitedByUser)
                .Include(x => x.Tenant)
                .Include(x => x.Project)
                .Include(x => x.ModulePermissions));

        if (!invites.Any())
        {
            return Array.Empty<ProjectInvitationWeb>();
        }

        return invites.Select(i => new ProjectInvitationWeb
        {
            InvitationId = i.Id,
            TenantId = i.TenantId,
            TenantName = i.Tenant?.Name ?? string.Empty,
            ProjectId = i.ProjectId!.Value,
            ProjectName = i.Project?.Name ?? string.Empty,
            Email = i.Email,
            IsAdmin = i.IsAdmin,
            Modules = i.ModulePermissions.Select(p => p.Module).ToList(),
            InvitedByUserEmail = i.InvitedByUser?.Email ?? string.Empty,
            InvitedByUserName = i.InvitedByUser is null
                ? string.Empty
                : $"{i.InvitedByUser.FirstName} {i.InvitedByUser.LastName}",
            CreatedAt = i.CreatedAt,
            ExpiresAt = i.ExpiresAt,
            Status = i.Status,
            Token = i.Token
        }).ToList();
    }
}
