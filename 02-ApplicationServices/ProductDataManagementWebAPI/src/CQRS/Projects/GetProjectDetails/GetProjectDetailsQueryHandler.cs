using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.GetProjectDetails
{
    public class GetProjectDetailsQueryHandler : IRequestHandler<GetProjectDetailsQuery, ProjectDetailsWeb>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly IReadRepository<ProjectCurrency> currencyRepo;
        private readonly ICurrentUser currentUser;

        public GetProjectDetailsQueryHandler(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            IReadRepository<ProjectCurrency> currencyRepo,
            ICurrentUser currentUser)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.currencyRepo = currencyRepo;
            this.currentUser = currentUser;
        }

        public async Task<ProjectDetailsWeb> Handle(GetProjectDetailsQuery request, CancellationToken cancellationToken)
        {
            // ─────────────────────────────────────────────────────────────────────
            // STEP 1: Load project with creator info
            // ─────────────────────────────────────────────────────────────────────
            Project project = await projectRepo.GetFirstBySearch(
                p => p.TenantId == request.TenantId && p.Id == request.ProjectId,
                include => include.Include(p => p.CreatedBy)
                                 .ThenInclude(cb => cb.User))
                ?? throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());

            // ─────────────────────────────────────────────────────────────────────
            // STEP 2: Determine user's role in tenant and project
            // ─────────────────────────────────────────────────────────────────────
            var tenantSnapshot = await currentUser.GetTenantSnapshotAsync(request.TenantId, cancellationToken);
            bool isSuperAdmin = currentUser.IsSuperAdmin;
            bool isTenantAdmin = tenantSnapshot?.IsTenantAdmin ?? false;

            // ─────────────────────────────────────────────────────────────────────
            // STEP 3: Load project members (for membership check and count)
            // ─────────────────────────────────────────────────────────────────────
            var allMembers = await projectMemberRepo.GetBySearch(
                pm => pm.ProjectId == request.ProjectId,
                include => include.Include(pm => pm.MemberRole));

            // Find current user's membership
            var projectMembership = allMembers.FirstOrDefault(pm => pm.UserId == currentUser.Id);

            // ─────────────────────────────────────────────────────────────────────
            // STEP 4: Determine user role code hierarchy
            // ─────────────────────────────────────────────────────────────────────
            // 1. Project membership role (if exists)
            // 2. Tenant Admin (if no project membership)
            // 3. System SuperAdmin (if no project membership or tenant admin)
            string userRoleCode;
            if (projectMembership != null)
            {
                userRoleCode = projectMembership.MemberRole?.Code ?? RoleCodes.ProjectViewer;
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
                // Fallback (shouldn't happen due to authorization)
                userRoleCode = RoleCodes.ProjectViewer;
            }

            // ─────────────────────────────────────────────────────────────────────
            // STEP 5: Get user's permissions for this project
            // ─────────────────────────────────────────────────────────────────────
            var userPermissions = new HashSet<string>();
            var projectSnapshot = await currentUser.GetProjectSnapshotAsync(request.ProjectId, cancellationToken);
            if (projectSnapshot != null)
            {
                userPermissions = projectSnapshot.ProjectPermissionCodes;
            }

            // ─────────────────────────────────────────────────────────────────────
            // STEP 6: Get project currency
            // ─────────────────────────────────────────────────────────────────────
            ProjectCurrency? currency = await currencyRepo.GetFirstBySearch(
                x => x.ProjectId == request.ProjectId,
                cancellationToken);

            return new ProjectDetailsWeb(
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
                MembersCount: allMembers.Count(),
                UserPermissions: userPermissions,
                Currency: currency is null
                    ? null
                    : new ProjectCurrencyWeb(currency.Code, currency.Name, currency.Symbol)
            );
        }
    }
}
