using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.GetUserTenants
{
    public class GetUserTenantsQueryHandler : IRequestHandler<GetUserTenantsQuery, IEnumerable<UserTenantWeb>>
    {
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly IRepository<TenantPreferencesProfile> preferencesRepo;
        private readonly ICurrentUser currentUser;

        public GetUserTenantsQueryHandler(
            IRepository<TenantMember> tenantMemberRepo,
            IRepository<TenantPreferencesProfile> preferencesRepo,
            ICurrentUser currentUser)
        {
            this.tenantMemberRepo = tenantMemberRepo;
            this.preferencesRepo = preferencesRepo;
            this.currentUser = currentUser;
        }

        public async Task<IEnumerable<UserTenantWeb>> Handle(GetUserTenantsQuery request, CancellationToken cancellationToken)
        {
            TenantPreferencesProfile? preferences = await preferencesRepo.GetFirstBySearch(
                p => p.UserId == currentUser.Id);

            Guid? activeTenantId = preferences?.ActiveTenantId;

            IEnumerable<TenantMember> memberships = await tenantMemberRepo.GetBySearch(
                m => m.UserId == currentUser.Id
                     && m.IsActive
                     && (m.MemberRole!.Code == RoleCodes.TenantAdmin || m.Tenant.IsActive),
                q => q.Include(m => m.Tenant).Include(m => m.MemberRole)
            );

            return memberships
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
