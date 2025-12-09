using Business.Interfaces.Model;
using Entities.Enums;
using Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Repositories.Repository.Interfaces;

namespace WebApi.Authorization
{
    public class TenantAdminHandler : AuthorizationHandler<TenantAdminRequirement>
    {
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly ICurrentUser currentUser;

        public TenantAdminHandler(IRepository<TenantMember> tenantMemberRepo, ICurrentUser currentUser)
        {
            this.tenantMemberRepo = tenantMemberRepo;
            this.currentUser = currentUser;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, TenantAdminRequirement requirement)
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
                if (httpContext.Request.RouteValues.TryGetValue("tenantId", out var raw) && raw is string s && Guid.TryParse(s, out var parsed))
                {
                    tenantId = parsed;
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
                m.Role == TenantRole.Admin);

            if (membership != null)
            {
                context.Succeed(requirement);
            }
        }
    }
}
