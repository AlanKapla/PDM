using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.GetAdminTenants
{
    public class GetAdminTenantsQueryHandler : IRequestHandler<GetAdminTenantsQuery, IEnumerable<TenantBasicWeb>>
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
                q => q.Include(m => m.Tenant)
            );

            return adminMemberships
                .Select(m => new TenantBasicWeb(
                    Id: m.TenantId,
                    Name: m.Tenant.Name,
                    CreatedAt: m.Tenant.CreatedAt,
                    IsActive: m.Tenant.IsActive
                ))
                .OrderBy(t => t.Name)
                .ToList();
        }
    }
}
