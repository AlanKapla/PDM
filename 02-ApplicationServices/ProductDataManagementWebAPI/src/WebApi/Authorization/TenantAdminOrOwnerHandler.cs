using Business.Interfaces.Model;
using Entities.Enums;
using Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Repositories.Repository.Interfaces;

namespace WebApi.Authorization
{
    /// <summary>
    /// Handler sprawdzający czy użytkownik jest adminem określonego tenanta.
    /// Nie wymaga aktywnego tenanta - pozwala administratorom zarządzać wszystkimi swoimi tenantami,
    /// włącznie z nieaktywnymi (np. reaktywacja).
    /// </summary>
    public class TenantAdminOrOwnerHandler : AuthorizationHandler<TenantAdminOrOwnerRequirement>
    {
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly ICurrentUser currentUser;

        public TenantAdminOrOwnerHandler(IRepository<TenantMember> tenantMemberRepo, ICurrentUser currentUser)
        {
            this.tenantMemberRepo = tenantMemberRepo;
            this.currentUser = currentUser;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, TenantAdminOrOwnerRequirement requirement)
        {
            // Użytkownik musi być zalogowany
            if (!currentUser.IsAuthenticated || currentUser.Id == Guid.Empty)
            {
                return;
            }

            // Pobierz tenantId z route
            var httpContext = context.Resource as HttpContext;
            Guid tenantId = Guid.Empty;
            
            if (httpContext != null)
            {
                if (httpContext.Request.RouteValues.TryGetValue("tenantId", out var raw) && 
                    raw is string s && 
                    Guid.TryParse(s, out var parsed))
                {
                    tenantId = parsed;
                }
            }

            if (tenantId == Guid.Empty)
            {
                return;
            }

            // Sprawdź czy użytkownik jest adminem tego tenanta (niezależnie od ActiveTenantId)
            var membership = await tenantMemberRepo.GetFirstBySearch(m =>
                m.TenantId == tenantId &&
                m.UserId == currentUser.Id &&
                m.IsActive &&
                m.Role == TenantRole.Admin);

            if (membership != null)
            {
                context.Succeed(requirement);
            }
        }
    }
}
