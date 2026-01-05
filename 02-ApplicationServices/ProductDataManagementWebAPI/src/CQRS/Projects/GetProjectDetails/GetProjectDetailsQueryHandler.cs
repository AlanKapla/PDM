using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.GetProjectDetails
{
    public class GetProjectDetailsQueryHandler : IRequestHandler<GetProjectDetailsQuery, ProjectDetailsWeb>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly ICurrentUser currentUser;

        public GetProjectDetailsQueryHandler(
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

        public async Task<ProjectDetailsWeb> Handle(GetProjectDetailsQuery request, CancellationToken cancellationToken)
        {
            Project project = await projectRepo.GetFirstBySearch(
                p => p.TenantId == request.TenantId && p.Id == request.ProjectId)
                ?? throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());

            // Get current user's project membership with role
            ProjectMember? projectMember = await projectMemberRepo.GetFirstBySearch(
                pm => pm.ProjectId == request.ProjectId && pm.UserId == currentUser.Id,
                include => include.Include(pm => pm.MemberRole)
            );

            // Get creator info separately
            TenantMember? creatorMember = await tenantMemberRepo.GetFirstBySearch(
                tm => tm.TenantId == request.TenantId 
                    && tm.UserId == project.CreatedByUserId,
                include => include.Include(tm => tm.User)
            );

            // Get members count
            IEnumerable<ProjectMember> allMembers = await projectMemberRepo.GetBySearch(
                pm => pm.ProjectId == request.ProjectId);

            // Determine user role code:
            // 1. If user has project membership -> use membership role
            // 2. If SuperAdmin without membership -> use SYSTEM.SUPERADMIN
            // 3. Otherwise -> ProjectViewer (fallback)
            string userRoleCode;
            if (projectMember != null)
            {
                // Has membership - use membership role (works for both regular users and SuperAdmins)
                userRoleCode = projectMember.MemberRole?.Code ?? RoleCodes.ProjectViewer;
            }
            else if (currentUser.IsSuperAdmin)
            {
                // SuperAdmin without membership - use SYSTEM.SUPERADMIN
                userRoleCode = RoleCodes.SystemSuperAdmin;
            }
            else
            {
                // Fallback for regular users without membership (shouldn't happen due to authorization)
                userRoleCode = RoleCodes.ProjectViewer;
            }

            // Get user's permissions for this project
            var userPermissions = new HashSet<string>();
            var projectSnapshot = await currentUser.GetProjectSnapshotAsync(request.ProjectId, cancellationToken);
            if (projectSnapshot != null)
            {
                userPermissions = projectSnapshot.ProjectPermissionCodes;
            }
            else
            {
                // If no project snapshot, check if user has tenant-level permissions
                var tenantSnapshot = await currentUser.GetActiveTenantSnapshotAsync(cancellationToken);
                if (tenantSnapshot != null && tenantSnapshot.IsTenantAdmin)
                {
                    // Tenant admin has access but through tenant permissions, not project permissions
                    // Return empty set - tenant permissions are separate
                    userPermissions = new HashSet<string>();
                }
            }

            return new ProjectDetailsWeb(
                Id: project.Id,
                TenantId: project.TenantId,
                Name: project.Name,
                IsActive: project.IsActive,
                CreatedAt: project.CreatedAt,
                CreatedByUserId: project.CreatedByUserId,
                CreatedByUserName: creatorMember?.User != null 
                    ? $"{creatorMember.User.FirstName} {creatorMember.User.LastName}".Trim()
                    : "Unknown",
                UserRoleCode: userRoleCode,
                MembersCount: allMembers.Count(),
                UserPermissions: userPermissions
            );
        }
    }
}
