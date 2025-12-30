using Business.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Authorization
{
    /// <summary>
    /// Sprawdza czy użytkownik jest członkiem projektu LUB administratorem tenanta.
    /// Administratorzy tenanta mają dostęp do wszystkich projektów w swoim tenancie.
    /// </summary>
    public class ProjectMemberOrAdminHandler : AuthorizationHandler<ProjectMemberOrAdminRequirement>
    {
        private readonly IAccessService accessService;

        public ProjectMemberOrAdminHandler(IAccessService accessService)
        {
            this.accessService = accessService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context, 
            ProjectMemberOrAdminRequirement requirement)
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

            if (await accessService.IsProjectMemberOrAdminAsync(tenantId, projectId))
            {
                context.Succeed(requirement);
            }
        }
    }
}
