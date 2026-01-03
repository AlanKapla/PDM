using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using CQRS.Extensions;
using Entities.Enums;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.UserTenants
{
    public class UserTenantsQueryHandler : IRequestHandler<UserTenantsQuery, IEnumerable<TenantDetailsWeb>>
    {
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly IRepository<TenantInvitation> invitationRepo;
        private readonly ICurrentUser currentUser;

        public UserTenantsQueryHandler(IRepository<TenantMember> tenantMemberRepo, IRepository<TenantInvitation> invitationRepo, ICurrentUser currentUser)
        {
            this.tenantMemberRepo = tenantMemberRepo;
            this.invitationRepo = invitationRepo;
            this.currentUser = currentUser;
        }

        public async Task<IEnumerable<TenantDetailsWeb>> Handle(UserTenantsQuery request, CancellationToken cancellationToken)
        {
            // Pobranie aktywnych członkostw użytkownika z filtrami w bazie:
            // - dla adminów: wszystkie tenanty (aktywne i nieaktywne)
            // - dla pozostałych: tylko aktywne tenanty
            IEnumerable<TenantMember> memberships = await tenantMemberRepo.GetBySearch(
                m => m.UserId == currentUser.Id 
                     && m.IsActive 
                     && m.Tenant != null 
                     && (m.MemberRole!.Code == RoleCodes.TenantAdmin || m.Tenant.IsActive),
                q => q.Include(m => m.Tenant).Include(m => m.MemberRole)
            );

            // Id tenantów gdzie użytkownik jest Adminem
            List<Guid> adminTenantIds = memberships
                .Where(m => m.MemberRole?.Code.IsTenantAdmin() == true)
                .Select(m => m.TenantId)
                .Distinct()
                .ToList();

            Dictionary<Guid, List<TenantMemberWeb>> membersPerTenant = new();
            Dictionary<Guid, List<TenantInvitationWeb>> invitationsPerTenant = new();

            if (adminTenantIds.Count > 0)
            {
                IEnumerable<TenantMember> adminTenantsMembers = await tenantMemberRepo.GetBySearch(
                    tm => adminTenantIds.Contains(tm.TenantId) && tm.IsActive,
                    q => q.Include(m => m.User).Include(m => m.MemberRole)
                );

                foreach (TenantMember member in adminTenantsMembers)
                {
                    if (!membersPerTenant.TryGetValue(member.TenantId, out List<TenantMemberWeb>? list))
                    {
                        list = new List<TenantMemberWeb>();
                        membersPerTenant[member.TenantId] = list;
                    }

                    list.Add(new TenantMemberWeb(
                        UserId: member.UserId,
                        Email: member.User.Email,
                        FirstName: member.User.FirstName,
                        LastName: member.User.LastName,
                        RoleCode: member.MemberRole?.Code ?? RoleCodes.TenantMember,
                        IsActive: member.IsActive,
                        JoinedAt: member.CreatedAt
                    ));
                }

                // Pending invitations for tenants current user administrates
                IEnumerable<TenantInvitation> invites = await invitationRepo.GetBySearch(
                    i => adminTenantIds.Contains(i.TenantId)
                         && i.IsActive
                         && i.Status == InvitationStatus.Pending
                         && i.ExpiresAt > DateTime.UtcNow,
                    q => q.Include(x => x.InvitedByUser)
                );

                foreach (TenantInvitation invite in invites)
                {
                    if (!invitationsPerTenant.TryGetValue(invite.TenantId, out List<TenantInvitationWeb>? list))
                    {
                        list = new List<TenantInvitationWeb>();
                        invitationsPerTenant[invite.TenantId] = list;
                    }

                    list.Add(new TenantInvitationWeb
                    {
                        InvitationId = invite.Id,
                        TenantId = invite.TenantId,
                        TenantName = string.Empty,
                        Email = invite.Email,
                        InvitedByUserEmail = invite.InvitedByUser?.Email ?? string.Empty,
                        InvitedByUserName = invite.InvitedByUser == null ? string.Empty : $"{invite.InvitedByUser.FirstName} {invite.InvitedByUser.LastName}",
                        CreatedAt = invite.CreatedAt,
                        ExpiresAt = invite.ExpiresAt,
                        Status = invite.Status,
                        Token = string.Empty
                    });
                }
            }

            return memberships
                .Select(m => new TenantDetailsWeb
                {
                    Id = m.TenantId,
                    Name = m.Tenant!.Name,
                    CreatedAt = m.Tenant.CreatedAt,
                    IsActive = m.Tenant.IsActive,
                    RoleCode = m.MemberRole?.Code ?? RoleCodes.TenantMember,
                    Members = membersPerTenant.TryGetValue(m.TenantId, out var members) ? members : new List<TenantMemberWeb>(),
                    Invitations = invitationsPerTenant.TryGetValue(m.TenantId, out var invs) ? invs : new List<TenantInvitationWeb>()
                })
                .ToList();
        }
    }
}
