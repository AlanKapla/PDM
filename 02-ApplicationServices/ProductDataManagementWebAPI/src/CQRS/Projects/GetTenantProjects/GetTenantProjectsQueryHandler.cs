using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.GetTenantProjects
{
    public class GetTenantProjectsQueryHandler : IRequestHandler<GetTenantProjectsQuery, IEnumerable<ProjectDetailsWeb>>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly ICurrentUser currentUser;

        public GetTenantProjectsQueryHandler(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            IRepository<TenantMember> tenantMemberRepo,
            ICurrentUser currentUser)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.currentUser = currentUser;
        }

        public async Task<IEnumerable<ProjectDetailsWeb>> Handle(GetTenantProjectsQuery request, CancellationToken cancellationToken)
        {
            // ─────────────────────────────────────────────────────────────────────
            // STEP 1: Determine user's role in tenant
            // ─────────────────────────────────────────────────────────────────────
            var tenantSnapshot = await currentUser.GetTenantSnapshotAsync(request.TenantId, cancellationToken);
            bool isSuperAdmin = currentUser.IsSuperAdmin;
            bool isTenantAdmin = tenantSnapshot?.IsTenantAdmin ?? false;

            // ─────────────────────────────────────────────────────────────────────
            // STEP 2: Load all projects with memberships in single query
            // ─────────────────────────────────────────────────────────────────────
            var allProjects = await projectRepo.GetBySearch(
                p => p.TenantId == request.TenantId,
                include => include.Include(p => p.CreatedBy)
                                 .ThenInclude(cb => cb.User));

            var projectIds = allProjects.Select(p => p.Id).ToList();

            // Load ALL project members in single query (for memberships + counts)
            var allProjectMembers = await projectMemberRepo.GetBySearch(
                pm => projectIds.Contains(pm.ProjectId),
                include => include.Include(pm => pm.MemberRole));

            // Filter in memory: user's project memberships
            var userProjectMemberships = allProjectMembers
                .Where(pm => pm.UserId == currentUser.Id)
                .ToList();

            var membershipDict = userProjectMemberships.ToDictionary(pm => pm.ProjectId);

            // Calculate members count from same data
            var membersCountDict = allProjectMembers
                .GroupBy(pm => pm.ProjectId)
                .ToDictionary(g => g.Key, g => g.Count());

            // ─────────────────────────────────────────────────────────────────────
            // STEP 3: Filter and build result based on user role
            // ─────────────────────────────────────────────────────────────────────
            var result = new List<ProjectDetailsWeb>();

            foreach (var project in allProjects)
            {
                // Determine if user should see this project
                bool hasProjectMembership = membershipDict.TryGetValue(project.Id, out var membership);
                bool isProjectAdmin = hasProjectMembership && membership.MemberRole?.Code == RoleCodes.ProjectAdmin;

                // Visibility rules:
                // - SuperAdmin → sees all projects
                // - Tenant Admin → sees all projects in their tenant
                // - Project Admin → sees all projects where they are admin (including inactive)
                // - Regular members → sees only active projects where they are members
                bool canSeeProject = isSuperAdmin 
                    || isTenantAdmin 
                    || isProjectAdmin 
                    || (hasProjectMembership && project.IsActive);

                if (!canSeeProject)
                    continue;

                // Determine user role code hierarchy:
                // 1. Project membership role (if exists)
                // 2. Tenant Admin (if no project membership)
                // 3. System SuperAdmin (if no project membership or tenant admin)
                string userRoleCode;
                if (hasProjectMembership)
                {
                    userRoleCode = membership!.MemberRole?.Code ?? RoleCodes.ProjectViewer;
                }
                else if (isTenantAdmin)
                {
                    userRoleCode = RoleCodes.TenantAdmin;
                }
                else if (isSuperAdmin)
                {
                    userRoleCode = RoleCodes.SystemSuperAdmin;
                }
                else
                {
                    // Should not happen due to visibility rules above
                    continue;
                }

                // Get user's permissions for this project
                var userPermissions = new HashSet<string>();
                var projectSnapshot = await currentUser.GetProjectSnapshotAsync(project.Id, cancellationToken);
                if (projectSnapshot != null)
                {
                    userPermissions = projectSnapshot.ProjectPermissionCodes;
                }

                int membersCount = membersCountDict.TryGetValue(project.Id, out int count) ? count : 0;

                result.Add(new ProjectDetailsWeb(
                    Id: project.Id,
                    TenantId: project.TenantId,
                    Name: project.Name,
                    IsActive: project.IsActive,
                    CreatedAt: project.CreatedAt,
                    CreatedByUserId: project.CreatedByUserId,
                    CreatedByUserName: project.CreatedBy?.User != null 
                        ? $"{project.CreatedBy.User.FirstName} {project.CreatedBy.User.LastName}".Trim()
                        : "Unknown",
                    UserRoleCode: userRoleCode,
                    MembersCount: membersCount,
                    UserPermissions: userPermissions
                ));
            }

            return result.OrderByDescending(p => p.CreatedAt).ToList();
        }
    }
}
