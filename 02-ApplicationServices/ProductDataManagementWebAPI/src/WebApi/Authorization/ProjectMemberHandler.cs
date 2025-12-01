using Business.Interfaces.Model;
using Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Repositories.Repository.Interfaces;

namespace WebApi.Authorization
{
    public class ProjectMemberHandler : AuthorizationHandler<ProjectMemberRequirement>
    {
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly ICurrentUser currentUser;

        public ProjectMemberHandler(
            IRepository<ProjectMember> projectMemberRepo, 
            IRepository<TenantMember> tenantMemberRepo,
            ICurrentUser currentUser)
        {
            this.projectMemberRepo = projectMemberRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.currentUser = currentUser;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context, 
            ProjectMemberRequirement requirement)
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
            Guid projectId = Guid.Empty;

            if (httpContext != null)
            {
                if (httpContext.Request.RouteValues.TryGetValue("tenantId", out var rawTenantId) && 
                    rawTenantId is string tenantString && 
                    Guid.TryParse(tenantString, out var parsedTenantId))
                {
                    tenantId = parsedTenantId;
                }

                if (httpContext.Request.RouteValues.TryGetValue("projectId", out var rawProjectId) && 
                    rawProjectId is string projectString && 
                    Guid.TryParse(projectString, out var parsedProjectId))
                {
                    projectId = parsedProjectId;
                }
            }

            if (tenantId == Guid.Empty || projectId == Guid.Empty)
            {
                return;
            }

            if (currentUser.ActiveTenantId != tenantId)
            {
                return;
            }

            // Weryfikacja aktywnego cz³onkostwa w tenancie
            TenantMember? tenantMembership = await tenantMemberRepo.GetFirstBySearch(
                m => m.TenantId == tenantId && m.UserId == currentUser.Id && m.IsActive);

            if (tenantMembership == null)
            {
                return;
            }

            // Weryfikacja cz³onkostwa w projekcie
            ProjectMember? projectMembership = await projectMemberRepo.GetFirstBySearch(
                pm => pm.TenantId == tenantId && 
                      pm.ProjectId == projectId && 
                      pm.UserId == currentUser.Id);

            if (projectMembership != null)
            {
                context.Succeed(requirement);
            }
        }
    }
}
