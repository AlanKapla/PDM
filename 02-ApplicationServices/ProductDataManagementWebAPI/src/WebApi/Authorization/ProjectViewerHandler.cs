using Business.Interfaces.Services;
using Entities.Enums;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Authorization
{
    /// <summary>
    /// Sprawdza czy użytkownik ma uprawnienia do przeglądania projektu (rola Viewer, Member, Editor lub Admin).
    /// </summary>
    public class ProjectViewerHandler : AuthorizationHandler<ProjectViewerRequirement>
    {
        private readonly IAccessService accessService;

        public ProjectViewerHandler(IAccessService accessService)
        {
            this.accessService = accessService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context, 
            ProjectViewerRequirement requirement)
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

            if (await accessService.HasProjectRoleAtLeastAsync(tenantId, projectId, ProjectRole.Viewer))
            {
                context.Succeed(requirement);
            }
        }
    }
}
