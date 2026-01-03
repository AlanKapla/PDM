using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.Projects;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.GetProjectMembers
{
    public class GetProjectMembersQueryHandler : IRequestHandler<GetProjectMembersQuery, IEnumerable<ProjectMemberWeb>>
    {
        private readonly IRepository<ProjectMember> projectMemberRepo;

        public GetProjectMembersQueryHandler(IRepository<ProjectMember> projectMemberRepo)
        {
            this.projectMemberRepo = projectMemberRepo;
        }

        public async Task<IEnumerable<ProjectMemberWeb>> Handle(GetProjectMembersQuery request, CancellationToken cancellationToken)
        {
            var projectMembers = await projectMemberRepo.GetBySearch(
                pm => pm.TenantId == request.TenantId 
                    && pm.ProjectId == request.ProjectId 
                    && pm.TenantMember.IsActive,
                include => include.Include(pm => pm.TenantMember)
                                  .ThenInclude(tm => tm.User)
                                  .Include(pm => pm.MemberRole)
            );

            var result = projectMembers
                .Select(pm => new ProjectMemberWeb(
                    UserId: pm.UserId,
                    Email: pm.TenantMember.User.Email,
                    FirstName: pm.TenantMember.User.FirstName,
                    LastName: pm.TenantMember.User.LastName,
                    RoleCode: pm.MemberRole?.Code ?? RoleCodes.ProjectMember,
                    JoinedAt: pm.JoinedAt
                ))
                .OrderBy(m => m.LastName)
                .ThenBy(m => m.FirstName);

            return result;
        }
    }
}
