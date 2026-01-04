using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Users;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Users.UserDetails
{
    public class UserDetailsQueryHandler : IRequestHandler<UserDetailsQuery, UserDetailsWeb>
    {
        private readonly ICurrentUser currentUser;
        private readonly IRepository<ProjectMember> projectMemberRepo;

        public UserDetailsQueryHandler(
            ICurrentUser currentUser,
            IRepository<ProjectMember> projectMemberRepo)
        {
            this.currentUser = currentUser;
            this.projectMemberRepo = projectMemberRepo;
        }

        public async Task<UserDetailsWeb> Handle(UserDetailsQuery request, CancellationToken cancellationToken)
        {
            var projectRoleCodes = new Dictionary<Guid, string>();
            var projectPermissions = new Dictionary<Guid, HashSet<string>>();
            var activeTenantPermissions = new HashSet<string>();

            if (currentUser.IsAuthenticated)
            {
                // Get active tenant permissions
                if (currentUser.ActiveTenantId.HasValue)
                {
                    var tenantSnapshot = await currentUser.GetActiveTenantSnapshotAsync(cancellationToken);
                    if (tenantSnapshot != null)
                    {
                        activeTenantPermissions = tenantSnapshot.TenantPermissionCodes;
                    }

                    // Get project memberships with permissions
                    var projectMemberships = await projectMemberRepo.GetBySearch(
                        pm => pm.UserId == currentUser.Id && 
                              pm.TenantId == currentUser.ActiveTenantId.Value &&
                              pm.Project.IsActive,
                        include => include.Include(pm => pm.Project).Include(pm => pm.MemberRole));

                    foreach (var pm in projectMemberships)
                    {
                        // Store role code
                        projectRoleCodes[pm.ProjectId] = pm.MemberRole?.Code ?? RoleCodes.ProjectMember;

                        // Get project permissions snapshot
                        var projectSnapshot = await currentUser.GetProjectSnapshotAsync(pm.ProjectId, cancellationToken);
                        if (projectSnapshot != null)
                        {
                            projectPermissions[pm.ProjectId] = projectSnapshot.ProjectPermissionCodes;
                        }
                        else
                        {
                            projectPermissions[pm.ProjectId] = new HashSet<string>();
                        }
                    }

                    // For SuperAdmin, add fallback permissions for projects without membership
                    // This allows UI to enable read-only features for non-member projects
                    if (currentUser.IsSuperAdmin)
                    {
                        // Get all active projects in tenant
                        var allProjects = await projectMemberRepo.GetBySearch(
                            pm => pm.TenantId == currentUser.ActiveTenantId.Value && pm.Project.IsActive,
                            include => include.Include(pm => pm.Project));

                        var allProjectIds = allProjects.Select(pm => pm.ProjectId).Distinct().ToList();
                        var memberProjectIds = projectMemberships.Select(pm => pm.ProjectId).ToHashSet();

                        // Add fallback permissions for projects where SuperAdmin is not a member
                        foreach (var projectId in allProjectIds)
                        {
                            if (!memberProjectIds.Contains(projectId))
                            {
                                // SuperAdmin fallback role
                                projectRoleCodes[projectId] = RoleCodes.SystemSuperAdmin;

                                // SuperAdmin fallback permissions (read-only)
                                projectPermissions[projectId] = new HashSet<string>(SuperAdminFallbackPermissions.ProjectReadOnly);
                            }
                        }
                    }
                }
            }

            return new UserDetailsWeb(
                currentUser.Id, 
                currentUser.FirstName, 
                currentUser.LastName, 
                currentUser.Email, 
                currentUser.ActiveTenantId,
                activeTenantPermissions,
                projectRoleCodes,
                projectPermissions);
        }
    }
}
