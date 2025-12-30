using Business.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Authorization
{
    public class TenantAdminHandler : AuthorizationHandler<TenantAdminRequirement>
    {
        private readonly IAccessService accessService;

        public TenantAdminHandler(IAccessService accessService)
        {
            this.accessService = accessService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, TenantAdminRequirement requirement)
        {
            if (!accessService.IsUserAuthenticated())
            {
                return;
            }

            if (!accessService.HasActiveTenant())
            {
                return;
            }

            var tenantId = accessService.GetRouteTenantId(context.Resource);

            if (tenantId == Guid.Empty)
            {
                return;
            }

            if (await accessService.IsTenantAdminAsync(tenantId))
            {
                context.Succeed(requirement);
            }
        }
    }
}
