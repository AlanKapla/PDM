using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.GetUserTenants
{
    public class GetUserTenantsQueryHandler : IRequestHandler<GetUserTenantsQuery, IEnumerable<UserTenantWeb>>
    {
        private readonly IReadRepository<Tenant> tenantRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly IRepository<TenantPreferencesProfile> preferencesRepo;
        private readonly ICurrentUser currentUser;

        public GetUserTenantsQueryHandler(
            IReadRepository<Tenant> tenantRepo,
            IRepository<TenantMember> tenantMemberRepo,
            IRepository<TenantPreferencesProfile> preferencesRepo,
            ICurrentUser currentUser)
        {
            this.tenantRepo = tenantRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.preferencesRepo = preferencesRepo;
            this.currentUser = currentUser;
        }

        public async Task<IEnumerable<UserTenantWeb>> Handle(GetUserTenantsQuery request, CancellationToken cancellationToken)
        {
            TenantPreferencesProfile? preferences = await preferencesRepo.GetFirstBySearch(
                p => p.UserId == currentUser.Id);

            Guid? activeTenantId = preferences?.ActiveTenantId;

            // SuperAdmin sees all tenants with membership roles where applicable
            if (currentUser.IsSuperAdmin)
            {
                // Get all tenants
                var allTenants = await tenantRepo.GetBySearch(_ => true);

                // Get user's memberships to show actual roles
                var memberships = await tenantMemberRepo.GetBySearch(
                    m => m.UserId == currentUser.Id && m.IsActive,
                    q => q.Include(m => m.MemberRole)
                );

                var membershipDict = memberships.ToDictionary(m => m.TenantId);

                return allTenants
                    .Select(t =>
                    {
                        // If has membership, use membership role; otherwise SystemSuperAdmin
                        string roleCode = membershipDict.TryGetValue(t.Id, out var membership)
                            ? (membership.MemberRole?.Code ?? RoleCodes.TenantMember)
                            : RoleCodes.SystemSuperAdmin;

                        return new UserTenantWeb(
                            Id: t.Id,
                            Name: t.Name,
                            CreatedAt: t.CreatedAt,
                            IsActive: t.IsActive,
                            RoleCode: roleCode,
                            IsActiveTenant: t.Id == activeTenantId
                        );
                    })
                    .OrderBy(t => t.Name)
                    .ToList();
            }

            // Regular users see only tenants where they are members (admins see inactive, members only active)
            IEnumerable<TenantMember> regularMemberships = await tenantMemberRepo.GetBySearch(
                m => m.UserId == currentUser.Id
                     && m.IsActive
                     && (m.MemberRole!.Code == RoleCodes.TenantAdmin || m.Tenant.IsActive),
                q => q.Include(m => m.Tenant).Include(m => m.MemberRole)
            );

            return regularMemberships
                .Select(m => new UserTenantWeb(
                    Id: m.TenantId,
                    Name: m.Tenant.Name,
                    CreatedAt: m.Tenant.CreatedAt,
                    IsActive: m.Tenant.IsActive,
                    RoleCode: m.MemberRole?.Code ?? RoleCodes.TenantMember,
                    IsActiveTenant: m.TenantId == activeTenantId
                ))
                .OrderBy(t => t.Name)
                .ToList();
        }
    }
}
