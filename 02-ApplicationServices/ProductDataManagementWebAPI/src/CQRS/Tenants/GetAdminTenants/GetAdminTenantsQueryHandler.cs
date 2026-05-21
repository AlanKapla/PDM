using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using Entities.Models.Tenants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.GetAdminTenants
{
    public sealed class GetAdminTenantsQueryHandler : IRequestHandler<GetAdminTenantsQuery, IEnumerable<TenantBasicWeb>>
    {
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly ICurrentUser currentUser;

        public GetAdminTenantsQueryHandler(
            IRepository<TenantMember> tenantMemberRepo,
            ICurrentUser currentUser)
        {
            this.tenantMemberRepo = tenantMemberRepo;
            this.currentUser = currentUser;
        }

        public async Task<IEnumerable<TenantBasicWeb>> Handle(GetAdminTenantsQuery request, CancellationToken cancellationToken)
        {
            IEnumerable<TenantMember> adminMemberships = await tenantMemberRepo.GetBySearch(
                m => m.UserId == currentUser.Id
                    && m.IsActive
                    && m.MemberRole!.Code == RoleCodes.TenantAdmin,
                include => include.Include(m => m.Tenant).Include(m => m.MemberRole)
            );

            return adminMemberships
                .Select(m => new TenantBasicWeb
                {
                    Id = m.Tenant.Id,
                    Name = m.Tenant.Name,
                    CreatedAt = m.Tenant.CreatedAt,
                    IsActive = m.Tenant.IsActive,
                    RoleCode = RoleCodes.TenantAdmin
                })
                .OrderBy(t => t.Name)
                .ToList();
        }
    }
}
