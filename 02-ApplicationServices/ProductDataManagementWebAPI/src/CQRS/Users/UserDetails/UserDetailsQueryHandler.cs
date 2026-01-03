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
