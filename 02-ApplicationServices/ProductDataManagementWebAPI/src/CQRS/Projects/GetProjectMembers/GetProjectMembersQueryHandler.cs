using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Projects;
using Entities.Models.Projects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.GetProjectMembers
{
    public sealed class GetProjectMembersQueryHandler : IRequestHandler<GetProjectMembersQuery, IEnumerable<ProjectMemberWeb>>
    {
        private readonly IUserService userService;
        private readonly IRepository<ProjectMember> projectMemberRepo;

        public GetProjectMembersQueryHandler(IUserService userService, IRepository<ProjectMember> projectMemberRepo)
        {
            this.userService = userService;
            this.projectMemberRepo = projectMemberRepo;
        }

        public async Task<IEnumerable<ProjectMemberWeb>> Handle(GetProjectMembersQuery request, CancellationToken cancellationToken)
        {
            List<ProjectMemberUserInfo> members = await userService.GetProjectMembersAsync(
                request.TenantId, request.ProjectId, cancellationToken);

            IEnumerable<ProjectMember> memberEntities = await projectMemberRepo.GetBySearch(
                pm => pm.TenantId == request.TenantId && pm.ProjectId == request.ProjectId && pm.IsActive,
                q => q.Include(pm => pm.ModulePermissions));

            Dictionary<Guid, ProjectMember> entityDict = memberEntities.ToDictionary(pm => pm.UserId);

            return members
                .Select(m =>
                {
                    entityDict.TryGetValue(m.UserId, out ProjectMember? entity);
                    return new ProjectMemberWeb
                    {
                        UserId = m.UserId,
                        Email = m.Email,
                        FirstName = m.FirstName,
                        LastName = m.LastName,
                        JoinedAt = m.JoinedAt,
                        IsAdmin = entity?.IsAdmin ?? false,
                        Modules = entity?.ModulePermissions
                            .Select(mp => (int)mp.Module)
                            .ToArray() ?? Array.Empty<int>()
                    };
                })
                .OrderBy(m => m.LastName)
                .ThenBy(m => m.FirstName);
        }
    }
}
