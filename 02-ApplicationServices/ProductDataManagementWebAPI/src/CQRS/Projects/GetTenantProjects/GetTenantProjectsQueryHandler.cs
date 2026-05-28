using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;
using Entities.Models.Projects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.GetTenantProjects
{
    public sealed class GetTenantProjectsQueryHandler : IRequestHandler<GetTenantProjectsQuery, IEnumerable<ProjectDetailsWeb>>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly ICurrentUser currentUser;

        public GetTenantProjectsQueryHandler(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.currentUser = currentUser;
        }

        public async Task<IEnumerable<ProjectDetailsWeb>> Handle(GetTenantProjectsQuery request, CancellationToken cancellationToken)
        {
            // STEP 1: Determine user's role in tenant
            TenantCtxSnapshot? tenantSnapshot = await currentUser.GetTenantSnapshotAsync(request.TenantId, cancellationToken);
            bool isSuperAdmin = currentUser.IsSuperAdmin;
            bool isTenantAdmin = tenantSnapshot?.IsAdmin ?? false;

            // STEP 2: Load all projects with creator info in a single query
            IEnumerable<Project> allProjects = await projectRepo.GetBySearch(
                p => p.TenantId == request.TenantId,
                include => include.Include(p => p.CreatedBy)
                                 .ThenInclude(cb => cb.User));

            List<Guid> projectIds = allProjects.Select(p => p.Id).ToList();

            // Load ALL project members in single query (for memberships + counts)
            IEnumerable<ProjectMember> allProjectMembers = await projectMemberRepo.GetBySearch(
                pm => projectIds.Contains(pm.ProjectId) && pm.TenantId == request.TenantId,
                include => include.Include(pm => pm.ModulePermissions));

            Dictionary<Guid, ProjectMember> membershipDict = allProjectMembers
                .Where(pm => pm.UserId == currentUser.Id)
                .ToDictionary(pm => pm.ProjectId);

            Dictionary<Guid, int> membersCountDict = allProjectMembers
                .GroupBy(pm => pm.ProjectId)
                .ToDictionary(g => g.Key, g => g.Count());

            // STEP 3: Filter and build result based on user role
            List<ProjectDetailsWeb> result = new List<ProjectDetailsWeb>();

            foreach (Project project in allProjects)
            {
                bool hasProjectMembership = membershipDict.TryGetValue(project.Id, out ProjectMember? membership);
                bool isProjectAdmin = hasProjectMembership && membership!.IsAdmin;

                // Visibility rules:
                // - SuperAdmin → all projects
                // - Tenant Admin → all projects in their tenant
                // - Project Admin → projects where they are admin (including inactive)
                // - Regular members → only active projects where they are members
                bool canSeeProject = isSuperAdmin
                    || isTenantAdmin
                    || isProjectAdmin
                    || (hasProjectMembership && project.IsActive);

                if (!canSeeProject)
                {
                    continue;
                }

                // NOTE: N+1 — per-project snapshot lookup. See PROJ-04 BLOKER report:
                // batch API on ICurrentUser/IUserContextCache is required to fix.
                HashSet<string> userPermissions = new HashSet<string>();
                ProjectCtxSnapshot? projectSnapshot = await currentUser.GetProjectSnapshotAsync(project.Id, cancellationToken);
                if (projectSnapshot is not null)
                {
                    userPermissions = projectSnapshot.ProjectPermissionCodes;
                }

                bool isAdmin;
                if (isTenantAdmin || isSuperAdmin)
                {
                    isAdmin = true;
                }
                else if (hasProjectMembership)
                {
                    isAdmin = membership!.IsAdmin;
                }
                else
                {
                    // Should not happen due to visibility rules above
                    continue;
                }

                int membersCount = membersCountDict.TryGetValue(project.Id, out int count) ? count : 0;

                result.Add(new ProjectDetailsWeb
                {
                    Id = project.Id,
                    TenantId = project.TenantId,
                    Name = project.Name,
                    IsActive = project.IsActive,
                    CreatedAt = project.CreatedAt,
                    CreatedByUserId = project.CreatedByUserId,
                    CreatedByUserName = project.CreatedBy?.User is not null
                        ? $"{project.CreatedBy.User.FirstName} {project.CreatedBy.User.LastName}".Trim()
                        : "Unknown",
                    IsAdmin = isAdmin,
                    CanViewAllResources = isAdmin || isTenantAdmin || isSuperAdmin,
                    MembersCount = membersCount,
                    UserPermissions = userPermissions,
                    Currency = null
                });
            }

            return result.OrderByDescending(p => p.CreatedAt).ToList();
        }
    }
}
