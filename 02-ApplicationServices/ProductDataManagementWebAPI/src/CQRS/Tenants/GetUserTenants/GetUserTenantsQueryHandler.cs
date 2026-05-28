using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using Entities.Models;
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
        private readonly ICurrentUser currentUser;

        public GetUserTenantsQueryHandler(
            IReadRepository<Tenant> tenantRepo,
            IRepository<TenantMember> tenantMemberRepo,
            IReadRepository<TenantPreferencesProfile> preferencesRepo,
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

            if (currentUser.IsSuperAdmin)
            {
                IEnumerable<Tenant> allTenants = await tenantRepo.GetBySearch(_ => true);

                IEnumerable<TenantMember> memberships = await tenantMemberRepo.GetBySearch(
                    m => m.UserId == currentUser.Id && m.IsActive
                );

                Dictionary<Guid, TenantMember> membershipDict = memberships.ToDictionary(m => m.TenantId);

                return allTenants
                    .Select(t =>
                    {
                        bool isAdmin = membershipDict.TryGetValue(t.Id, out TenantMember? membership)
                            ? membership.IsAdmin
                            : false;

                        return new UserTenantWeb
                        {
                            Id = t.Id,
                            Name = t.Name,
                            CreatedAt = t.CreatedAt,
                            IsActive = t.IsActive,
                            IsAdmin = isAdmin,
                            IsActiveTenant = t.Id == activeTenantId
                        };
                    })
                    .OrderBy(t => t.Name)
                    .ToList();
            }

            IEnumerable<TenantMember> regularMemberships = await tenantMemberRepo.GetBySearch(
                m => m.UserId == currentUser.Id
                     && m.IsActive
                     && (m.IsAdmin || m.Tenant.IsActive),
                q => q.Include(m => m.Tenant)
            );

            return regularMemberships
                .Select(m => new UserTenantWeb
                {
                    Id = m.TenantId,
                    Name = m.Tenant.Name,
                    CreatedAt = m.Tenant.CreatedAt,
                    IsActive = m.Tenant.IsActive,
                    IsAdmin = m.IsAdmin,
                    IsActiveTenant = m.TenantId == activeTenantId
                })
                .OrderBy(t => t.Name)
                .ToList();
        }
    }
}
