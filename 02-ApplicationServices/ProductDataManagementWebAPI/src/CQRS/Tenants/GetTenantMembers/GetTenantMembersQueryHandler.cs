using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.Tenants;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
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
                include => include.Include(tm => tm.User).Include(tm => tm.MemberRole)
            );

            var result = tenantMembers
                .Select(tm => new TenantMemberWeb(
                    UserId: tm.UserId,
                    Email: tm.User.Email,
                    FirstName: tm.User.FirstName,
                    LastName: tm.User.LastName,
                    RoleCode: tm.MemberRole?.Code ?? RoleCodes.TenantMember,
                    IsActive: tm.IsActive,
                    JoinedAt: tm.CreatedAt
                ))
                .OrderBy(m => m.LastName)
                .ThenBy(m => m.FirstName);

            return result;
        }
    }
}
