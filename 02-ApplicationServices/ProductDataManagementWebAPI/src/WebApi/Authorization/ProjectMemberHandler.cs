using Business.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Authorization
{
    public class ProjectMemberHandler : AuthorizationHandler<ProjectMemberRequirement>
    {
        private readonly IAccessService accessService;

        public ProjectMemberHandler(IAccessService accessService)
        {
            this.accessService = accessService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context, 
            ProjectMemberRequirement requirement)
        {
            if (!accessService.IsUserAuthenticated())
            {
                return;
            }

            if (!accessService.HasActiveTenant())
            {
                return;
            }

            var (tenantId, projectId) = accessService.GetRouteIds(context.Resource);

            if (tenantId == Guid.Empty || projectId == Guid.Empty)
            {
                return;
            }

            if (await accessService.IsProjectMemberAsync(tenantId, projectId))
            {
                context.Succeed(requirement);
            }
        }
    }
}
