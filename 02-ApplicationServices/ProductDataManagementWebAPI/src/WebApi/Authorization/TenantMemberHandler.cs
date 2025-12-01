using Business.Interfaces.Model;
using Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Repositories.Repository.Interfaces;

namespace WebApi.Authorization
{
    public class TenantMemberHandler : AuthorizationHandler<TenantMemberRequirement>
    {
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly ICurrentUser currentUser;

        public TenantMemberHandler(IRepository<TenantMember> tenantMemberRepo, ICurrentUser currentUser)
        {
            this.tenantMemberRepo = tenantMemberRepo;
            this.currentUser = currentUser;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, TenantMemberRequirement requirement)
        {
            if (!currentUser.IsAuthenticated || currentUser.Id == Guid.Empty)
            {
                return;
            }

            if (!currentUser.ActiveTenantId.HasValue)
            {
                return;
            }

            var httpContext = context.Resource as HttpContext;
            Guid tenantId = Guid.Empty;
            
            if (httpContext != null)
            {
                if (httpContext.Request.RouteValues.TryGetValue("tenantId", out var routeTenantId) && 
                    routeTenantId is string routeString && 
                    Guid.TryParse(routeString, out var routeParsed))
                {
                    tenantId = routeParsed;
                }
                else if (httpContext.Request.Query.TryGetValue("tenantId", out var queryTenantId) &&
                         Guid.TryParse(queryTenantId.FirstOrDefault(), out var queryParsed))
                {
                    tenantId = queryParsed;
                }
            }

            if (tenantId == Guid.Empty)
            {
                return;
            }

            if (currentUser.ActiveTenantId != tenantId)
            {
                return;
            }

            var membership = await tenantMemberRepo.GetFirstBySearch(m => 
                m.TenantId == tenantId && 
                m.UserId == currentUser.Id && 
                m.IsActive);
            
            if (membership != null)
            {
                context.Succeed(requirement);
            }
        }
    }
}