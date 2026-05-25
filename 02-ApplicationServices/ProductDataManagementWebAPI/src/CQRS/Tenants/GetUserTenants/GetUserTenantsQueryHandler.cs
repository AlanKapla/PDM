using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using Entities.Models;
using Entities.Models.Subscriptions;
using Entities.Models.Tenants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.GetUserTenants
{
    public sealed class GetUserTenantsQueryHandler : IRequestHandler<GetUserTenantsQuery, IEnumerable<UserTenantWeb>>
    {
        private readonly IReadRepository<Tenant> tenantRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly IReadRepository<TenantPreferencesProfile> preferencesRepo;
        private readonly IReadRepository<TenantSubscription> subscriptionRepo;
        private readonly ICurrentUser currentUser;

        public GetUserTenantsQueryHandler(
            IReadRepository<Tenant> tenantRepo,
            IRepository<TenantMember> tenantMemberRepo,
            IReadRepository<TenantPreferencesProfile> preferencesRepo,
            IReadRepository<TenantSubscription> subscriptionRepo,
            ICurrentUser currentUser)
        {
            this.tenantRepo = tenantRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.preferencesRepo = preferencesRepo;
            this.subscriptionRepo = subscriptionRepo;
            this.currentUser = currentUser;
        }

        public async Task<IEnumerable<UserTenantWeb>> Handle(GetUserTenantsQuery request, CancellationToken cancellationToken)
        {
            TenantPreferencesProfile? preferences = await preferencesRepo.GetFirstBySearch(
                p => p.UserId == currentUser.Id);

            Guid? activeTenantId = preferences?.ActiveTenantId;

            if (currentUser.IsSuperAdmin)
            {
                IEnumerable<Tenant> allTenants = await tenantRepo.GetBySearch(_ => true);

                IEnumerable<TenantMember> memberships = await tenantMemberRepo.GetBySearch(
                    m => m.UserId == currentUser.Id && m.IsActive,
                    q => q.Include(m => m.MemberRole)
                );

                Dictionary<Guid, TenantMember> membershipDict = memberships.ToDictionary(m => m.TenantId);

                List<Guid> tenantIds = allTenants.Select(t => t.Id).ToList();
                IEnumerable<TenantSubscription> subscriptions = await subscriptionRepo.GetBySearch(
                    s => tenantIds.Contains(s.TenantId));
                Dictionary<Guid, TenantSubscription> subscriptionByTenantId = subscriptions.ToDictionary(s => s.TenantId);

                return allTenants
                    .Select(t =>
                    {
                        string roleCode = membershipDict.TryGetValue(t.Id, out TenantMember? membership)
                            ? (membership.MemberRole?.Code ?? RoleCodes.TenantMember)
                            : RoleCodes.SystemSuperAdmin;

                        return new UserTenantWeb
                        {
                            Id = t.Id,
                            Name = t.Name,
                            CreatedAt = t.CreatedAt,
                            IsActive = t.IsActive,
                            RoleCode = roleCode,
                            IsActiveTenant = t.Id == activeTenantId,
                            SubscriptionStatus = subscriptionByTenantId.TryGetValue(t.Id, out TenantSubscription? sub) ? sub.Status : null
                        };
                    })
                    .OrderBy(t => t.Name)
                    .ToList();
            }

            IEnumerable<TenantMember> regularMemberships = await tenantMemberRepo.GetBySearch(
                m => m.UserId == currentUser.Id
                     && m.IsActive
                     && (m.MemberRole!.Code == RoleCodes.TenantAdmin || m.Tenant.IsActive),
                q => q.Include(m => m.Tenant).Include(m => m.MemberRole)
            );

            List<Guid> regularTenantIds = regularMemberships.Select(m => m.TenantId).ToList();
            IEnumerable<TenantSubscription> regularSubscriptions = await subscriptionRepo.GetBySearch(
                s => regularTenantIds.Contains(s.TenantId));
            Dictionary<Guid, TenantSubscription> regularSubscriptionByTenantId = regularSubscriptions.ToDictionary(s => s.TenantId);

            return regularMemberships
                .Select(m => new UserTenantWeb
                {
                    Id = m.TenantId,
                    Name = m.Tenant.Name,
                    CreatedAt = m.Tenant.CreatedAt,
                    IsActive = m.Tenant.IsActive,
                    RoleCode = m.MemberRole?.Code ?? RoleCodes.TenantMember,
                    IsActiveTenant = m.TenantId == activeTenantId,
                    SubscriptionStatus = regularSubscriptionByTenantId.TryGetValue(m.TenantId, out TenantSubscription? sub) ? sub.Status : null
                })
                .OrderBy(t => t.Name)
                .ToList();
        }
    }
}
