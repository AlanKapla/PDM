using Business.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Authorization
{
    /// <summary>
    /// Handler sprawdzający czy użytkownik jest adminem określonego tenanta.
    /// Nie wymaga aktywnego tenanta - pozwala administratorom zarządzać wszystkimi swoimi tenantami,
    /// włącznie z nieaktywnymi (np. reaktywacja).
    /// </summary>
    public class TenantAdminOrOwnerHandler : AuthorizationHandler<TenantAdminOrOwnerRequirement>
    {
        private readonly IAccessService accessService;

        public TenantAdminOrOwnerHandler(IAccessService accessService)
        {
            this.accessService = accessService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, TenantAdminOrOwnerRequirement requirement)
        {
            if (!accessService.IsUserAuthenticated())
            {
                return;
            }

            var tenantId = accessService.GetRouteTenantId(context.Resource);

            if (tenantId == Guid.Empty)
            {
                return;
            }

            if (await accessService.IsTenantAdminOrOwnerAsync(tenantId))
            {
                context.Succeed(requirement);
            }
        }
    }
}
