using Business.Interfaces.Constants;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Projects;
using MediatR;

namespace CQRS.Projects.GetProjectMembers
{
    public class GetProjectMembersQueryHandler : IRequestHandler<GetProjectMembersQuery, IEnumerable<ProjectMemberWeb>>
    {
        private readonly IUserService userService;

        public GetProjectMembersQueryHandler(IUserService userService)
        {
            this.userService = userService;
        }

        public async Task<IEnumerable<ProjectMemberWeb>> Handle(GetProjectMembersQuery request, CancellationToken cancellationToken)
        {
            var members = await userService.GetProjectMembersAsync(
                request.TenantId, request.ProjectId, cancellationToken);

            return members
                .Select(m => new ProjectMemberWeb(
                    UserId: m.UserId,
                    Email: m.Email,
                    FirstName: m.FirstName,
                    LastName: m.LastName,
                    RoleCode: m.RoleCode ?? RoleCodes.ProjectViewer,
                    JoinedAt: m.JoinedAt
                ))
                .OrderBy(m => m.LastName)
                .ThenBy(m => m.FirstName);
        }
    }
}
