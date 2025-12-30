using Business.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Authorization
{
    /// <summary>
    /// Sprawdza czy użytkownik ma uprawnienia do edycji zasobów w projekcie (rola Editor lub Admin).
    /// Nie sprawdza uprawnień do konkretnego zasobu - tylko rolę w projekcie.
    /// </summary>
    public class ProjectEditorHandler : AuthorizationHandler<ProjectEditorRequirement>
    {
        private readonly IAccessService accessService;

        public ProjectEditorHandler(IAccessService accessService)
        {
            this.accessService = accessService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ProjectEditorRequirement requirement)
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

            if (await accessService.CanEditProjectAsync(tenantId, projectId))
            {
                context.Succeed(requirement);
            }
        }
    }
}
