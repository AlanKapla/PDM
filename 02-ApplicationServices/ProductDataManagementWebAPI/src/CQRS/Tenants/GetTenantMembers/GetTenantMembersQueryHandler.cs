using Business.Interfaces.WebModels.Tenants;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.GetTenantMembers
{
    public class GetTenantMembersQueryHandler : IRequestHandler<GetTenantMembersQuery, IEnumerable<TenantMemberWeb>>
    {
        private readonly IRepository<TenantMember> tenantMemberRepo;

        public GetTenantMembersQueryHandler(IRepository<TenantMember> tenantMemberRepo)
        {
            this.tenantMemberRepo = tenantMemberRepo;
        }

        public async Task<IEnumerable<TenantMemberWeb>> Handle(GetTenantMembersQuery request, CancellationToken cancellationToken)
        {
            var tenantMembers = await tenantMemberRepo.GetBySearch(
                tm => tm.TenantId == request.TenantId && tm.IsActive,
                include => include.Include(tm => tm.User)
            );

            var result = tenantMembers
                .Select(tm => new TenantMemberWeb(
                    UserId: tm.UserId,
                    Email: tm.User.Email,
                    FirstName: tm.User.FirstName,
                    LastName: tm.User.LastName,
                    Role: tm.Role,
                    IsActive: tm.IsActive,
                    JoinedAt: tm.CreatedAt
                ))
                .OrderBy(m => m.LastName)
                .ThenBy(m => m.FirstName);

            return result;
        }
    }
}
