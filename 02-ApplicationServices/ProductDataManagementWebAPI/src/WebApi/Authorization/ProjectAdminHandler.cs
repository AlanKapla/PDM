using Business.Interfaces.Model;
using Entities.Enums;
using Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Repositories.Repository.Interfaces;

namespace WebApi.Authorization
{
    public class ProjectAdminHandler : AuthorizationHandler<ProjectAdminRequirement>
    {
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;

        private readonly ICurrentUser currentUser;

        public ProjectAdminHandler(IRepository<ProjectMember> projectMemberRepo, ICurrentUser currentUser, IRepository<TenantMember> tenantMemberRepo)
        {
            this.projectMemberRepo = projectMemberRepo;
            this.currentUser = currentUser;
            this.tenantMemberRepo = tenantMemberRepo;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ProjectAdminRequirement requirement)
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

            TenantMember? tenantMembership = await tenantMemberRepo.GetFirstBySearch(m => m.TenantId == tenantId && m.UserId == currentUser.Id && m.IsActive);

            ProjectMember? projectMembership = await projectMemberRepo.GetFirstBySearch(
                pm => pm.TenantId == tenantId && 
                      pm.ProjectId == projectId && 
                      pm.UserId == currentUser.Id);

            if (projectMembership != null && projectMembership.Role == ProjectRole.Admin 
                && tenantMembership != null && tenantMembership.Role == TenantRole.Admin)
            {
                context.Succeed(requirement);
            }
        }
    }
}
