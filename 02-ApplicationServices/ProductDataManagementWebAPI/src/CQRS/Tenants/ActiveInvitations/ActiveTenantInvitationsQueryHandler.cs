using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.ActiveInvitations
{
    public class ActiveTenantInvitationsQueryHandler : IRequestHandler<ActiveTenantInvitationsQuery, IEnumerable<TenantInvitationWeb>>
    {
        private readonly IRepository<TenantInvitation> invitationRepo;
        private readonly IRepository<Tenant> tenantRepo;
        private readonly ICurrentUser currentUser;

        public ActiveTenantInvitationsQueryHandler(IRepository<TenantInvitation> invitationRepo, IRepository<Tenant> tenantRepo, ICurrentUser currentUser)
        {
            this.invitationRepo = invitationRepo;
            this.tenantRepo = tenantRepo;
            this.currentUser = currentUser;
        }

        public async Task<IEnumerable<TenantInvitationWeb>> Handle(ActiveTenantInvitationsQuery request, CancellationToken cancellationToken)
        {
            string email = currentUser.Email.Trim().ToLowerInvariant();
            IEnumerable<TenantInvitation> invites = await invitationRepo.GetBySearch(
                i => i.IsActive && i.Email.ToLower() == email && i.Status == InvitationStatus.Pending && i.ExpiresAt > DateTime.UtcNow,
                q => q.Include(x => x.InvitedByUser)
            );

            if (!invites.Any())
            {
                return Array.Empty<TenantInvitationWeb>();
            }

            // Load tenants for mapping names
            HashSet<Guid> tenantIds = invites.Select(i => i.TenantId).ToHashSet();
            IEnumerable<Tenant> tenants = await tenantRepo.GetBySearch(t => tenantIds.Contains(t.Id));
            Dictionary<Guid, string> nameById = tenants.ToDictionary(t => t.Id, t => t.Name);

            return invites.Select(i => new TenantInvitationWeb
            {
                InvitationId = i.Id,
                TenantId = i.TenantId,
                TenantName = nameById.TryGetValue(i.TenantId, out var name) ? name : string.Empty,
                Email = i.Email,
                InvitedByUserEmail = i.InvitedByUser?.Email ?? string.Empty,
                InvitedByUserName = i.InvitedByUser == null ? string.Empty : $"{i.InvitedByUser.FirstName} {i.InvitedByUser.LastName}",
                CreatedAt = i.CreatedAt,
                ExpiresAt = i.ExpiresAt,
                Status = i.Status,
                Token = i.Token
            });
        }
    }
}
