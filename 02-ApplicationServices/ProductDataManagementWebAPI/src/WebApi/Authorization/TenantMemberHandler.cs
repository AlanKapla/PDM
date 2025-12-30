using Business.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Authorization
{
    public class TenantMemberHandler : AuthorizationHandler<TenantMemberRequirement>
    {
        private readonly IAccessService accessService;

        public TenantMemberHandler(IAccessService accessService)
        {
            this.accessService = accessService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, TenantMemberRequirement requirement)
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
                // Fallback: try to get from query string
                if (context.Resource is HttpContext httpContext &&
                    httpContext.Request.Query.TryGetValue("tenantId", out var queryTenantId) &&
                    Guid.TryParse(queryTenantId.FirstOrDefault(), out var queryParsed))
                {
                    tenantId = queryParsed;
                }
            }

            if (tenantId == Guid.Empty)
            {
                return;
            }

            if (await accessService.IsTenantMemberAsync(tenantId))
            {
                context.Succeed(requirement);
            }
        }
    }
}
