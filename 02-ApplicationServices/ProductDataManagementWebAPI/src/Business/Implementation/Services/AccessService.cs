using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Enums;
using Entities.Models;
using Microsoft.AspNetCore.Http;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public class AccessService : IAccessService
    {
        private readonly ICurrentUser currentUser;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly IRepository<ProjectFile> projectFileRepo;
        private readonly IRepository<SharedProjectFile> sharedProjectFileRepo;

        public AccessService(
            ICurrentUser currentUser,
            IRepository<TenantMember> tenantMemberRepo,
            IRepository<ProjectMember> projectMemberRepo,
            IRepository<ProjectFile> projectFileRepo,
            IRepository<SharedProjectFile> sharedProjectFileRepo)
        {
            this.currentUser = currentUser;
            this.tenantMemberRepo = tenantMemberRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.projectFileRepo = projectFileRepo;
            this.sharedProjectFileRepo = sharedProjectFileRepo;
        }

        public bool IsActiveTenant(Guid tenantId)
        {
            if (!IsUserAuthenticated())
            {
                return false;
            }

            return currentUser.ActiveTenantId.HasValue && currentUser.ActiveTenantId.Value == tenantId;
        }

        public async Task<bool> IsTenantMemberAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            if (!ValidateUserAndTenant(tenantId))
            {
                return false;
            }

            var membership = await GetTenantMembershipAsync(tenantId);
            return membership != null;
        }

        public async Task<bool> IsTenantAdminAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            if (!ValidateUserAndTenant(tenantId))
            {
                return false;
            }

            var membership = await tenantMemberRepo.GetFirstBySearch(
                m => m.TenantId == tenantId &&
                     m.UserId == currentUser.Id &&
                     m.IsActive &&
                     m.Role == TenantRole.Admin);

            return membership != null;
        }

        public async Task<bool> IsTenantAdminOrOwnerAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            // Special case: Does NOT validate ActiveTenantId
            // Allows managing ALL tenants where user is admin, including inactive ones
            if (!IsUserAuthenticated())
            {
                return false;
            }

            var membership = await tenantMemberRepo.GetFirstBySearch(
                m => m.TenantId == tenantId &&
                     m.UserId == currentUser.Id &&
                     m.IsActive &&
                     m.Role == TenantRole.Admin);

            return membership != null;
        }

        public async Task<bool> IsProjectMemberAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken = default)
        {
            if (!ValidateUserAndTenant(tenantId))
            {
                return false;
            }

            if (!await HasTenantMembershipAsync(tenantId))
            {
                return false;
            }

            var projectMembership = await GetProjectMembershipAsync(tenantId, projectId);
            return projectMembership != null;
        }

        public async Task<bool> IsProjectAdminAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken = default)
        {
            if (!ValidateUserAndTenant(tenantId))
            {
                return false;
            }

            if (!await HasTenantMembershipAsync(tenantId))
            {
                return false;
            }

            var projectMembership = await projectMemberRepo.GetFirstBySearch(
                pm => pm.TenantId == tenantId &&
                      pm.ProjectId == projectId &&
                      pm.UserId == currentUser.Id &&
                      pm.Role == ProjectRole.Admin);

            return projectMembership != null;
        }

        public async Task<bool> IsProjectMemberOrAdminAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken = default)
        {
            if (!ValidateUserAndTenant(tenantId))
            {
                return false;
            }

            // Check if user is tenant admin
            if (await IsProjectAdminAsync(tenantId, projectId, cancellationToken))
            {
                return true;
            }

            // Check if user is project member
            return await IsProjectMemberAsync(tenantId, projectId, cancellationToken);
        }

        public async Task<ProjectRole?> GetProjectRoleAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken = default)
        {
            if (!ValidateUserAndTenant(tenantId))
            {
                return null;
            }

            var projectMembership = await GetProjectMembershipAsync(tenantId, projectId);
            return projectMembership?.Role;
        }

        public async Task<bool> HasProjectRoleAtLeastAsync(Guid tenantId, Guid projectId, ProjectRole minimumRole, CancellationToken cancellationToken = default)
        {
            bool isProjectAdmin = await IsProjectAdminAsync(tenantId, projectId, cancellationToken);

            if (isProjectAdmin)
            {
                return true;
            }

            var currentRole = await GetProjectRoleAsync(tenantId, projectId, cancellationToken);

            if (!currentRole.HasValue)
            {
                return false;
            }

            return GetRoleLevel(currentRole.Value) <= GetRoleLevel(minimumRole);
        }

        public async Task<bool> CanEditProjectAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken = default)
        {
            return await HasProjectRoleAtLeastAsync(tenantId, projectId, ProjectRole.Editor, cancellationToken);
        }

        public async Task<bool> CanEditProjectFileAsync(Guid tenantId, Guid projectId, Guid fileId, CancellationToken cancellationToken = default)
        {
            // First check if user has Editor/Admin role in the project
            if (!await CanEditProjectAsync(tenantId, projectId, cancellationToken))
            {
                return false;
            }

            // Check if user owns the file
            var file = await projectFileRepo.GetFirstBySearch(
                f => f.Id == fileId &&
                     f.TenantId == tenantId &&
                     f.ProjectId == projectId &&
                     !f.IsDeleted);

            if (file == null)
            {
                return false;
            }

            // User owns the file
            if (file.OwnerId == currentUser.Id)
            {
                return true;
            }

            // Check if file is shared with the user
            var sharedFile = await sharedProjectFileRepo.GetFirstBySearch(
                sf => sf.ProjectFileId == fileId &&
                      sf.TenantId == tenantId &&
                      sf.ProjectId == projectId &&
                      sf.SharedWithUserId == currentUser.Id);

            return sharedFile != null;
        }

        public (Guid TenantId, Guid ProjectId) GetRouteIds(object? httpContextResource)
        {
            if (httpContextResource is not HttpContext httpContext)
            {
                return (Guid.Empty, Guid.Empty);
            }

            Guid tenantId = Guid.Empty;
            Guid projectId = Guid.Empty;

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

            return (tenantId, projectId);
        }

        public Guid GetRouteTenantId(object? httpContextResource)
        {
            if (httpContextResource is not HttpContext httpContext)
            {
                return Guid.Empty;
            }

            if (httpContext.Request.RouteValues.TryGetValue("tenantId", out var rawTenantId) &&
                rawTenantId is string tenantString &&
                Guid.TryParse(tenantString, out var parsedTenantId))
            {
                return parsedTenantId;
            }

            return Guid.Empty;
        }

        public bool IsUserAuthenticated()
        {
            return currentUser.IsAuthenticated && currentUser.Id != Guid.Empty;
        }

        public bool HasActiveTenant()
        {
            return currentUser.ActiveTenantId.HasValue;
        }

        private bool ValidateUserAndTenant(Guid tenantId)
        {
            return IsUserAuthenticated() && IsActiveTenant(tenantId);
        }

        private async Task<TenantMember?> GetTenantMembershipAsync(Guid tenantId)
        {
            return await tenantMemberRepo.GetFirstBySearch(
                m => m.TenantId == tenantId &&
                     m.UserId == currentUser.Id &&
                     m.IsActive &&
                     m.Tenant.IsActive);
        }

        private async Task<bool> HasTenantMembershipAsync(Guid tenantId)
        {
            var membership = await GetTenantMembershipAsync(tenantId);
            return membership != null;
        }

        private async Task<ProjectMember?> GetProjectMembershipAsync(Guid tenantId, Guid projectId)
        {
            return await projectMemberRepo.GetFirstBySearch(
                pm => pm.TenantId == tenantId &&
                      pm.ProjectId == projectId &&
                      pm.UserId == currentUser.Id &&
                      pm.Project.IsActive);
        }

        private static int GetRoleLevel(ProjectRole role)
        {
            return role switch
            {
                ProjectRole.Admin => 0,
                ProjectRole.Editor => 1,
                ProjectRole.Viewer => 2,
                ProjectRole.Member => 3,
                _ => int.MaxValue
            };
        }
    }
}
