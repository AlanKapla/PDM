using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using Entities.Models.Tenants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.ActiveInvitations
{
    public sealed class ActiveTenantInvitationsQueryHandler : IRequestHandler<ActiveTenantInvitationsQuery, IEnumerable<TenantInvitationWeb>>
    {
        private readonly IReadRepository<TenantInvitation> invitationRepo;
        private readonly ICurrentUser currentUser;

        public ActiveTenantInvitationsQueryHandler(
            IReadRepository<TenantInvitation> invitationRepo,
            ICurrentUser currentUser)
        {
            this.invitationRepo = invitationRepo;
            this.currentUser = currentUser;
        }

        public async Task<IEnumerable<TenantInvitationWeb>> Handle(ActiveTenantInvitationsQuery request, CancellationToken cancellationToken)
        {
            string email = currentUser.Email.Trim().ToLowerInvariant();
            IEnumerable<TenantInvitation> invites = await invitationRepo.GetBySearch(
                i => i.IsActive && i.Email.ToLower() == email && i.Status == InvitationStatus.Pending && i.ExpiresAt > DateTime.UtcNow,
                q => q.Include(x => x.InvitedByUser).Include(x => x.Tenant)
            );

            if (!invites.Any())
            {
                return Array.Empty<TenantInvitationWeb>();
            }

            return invites.Select(i => new TenantInvitationWeb
            {
                InvitationId = i.Id,
                TenantId = i.TenantId,
                TenantName = i.Tenant?.Name ?? string.Empty,
                Email = i.Email,
                InvitedByUserEmail = i.InvitedByUser?.Email ?? string.Empty,
                InvitedByUserName = i.InvitedByUser is null ? string.Empty : $"{i.InvitedByUser.FirstName} {i.InvitedByUser.LastName}",
                CreatedAt = i.CreatedAt,
                ExpiresAt = i.ExpiresAt,
                Status = i.Status,
                Token = i.Token
            });
        }
    }
}
