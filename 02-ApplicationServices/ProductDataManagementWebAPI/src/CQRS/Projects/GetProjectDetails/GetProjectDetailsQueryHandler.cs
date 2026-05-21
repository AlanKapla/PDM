using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;
using Entities.Models.Projects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.GetProjectDetails
{
    public sealed class GetProjectDetailsQueryHandler : IRequestHandler<GetProjectDetailsQuery, ProjectDetailsWeb>
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
            // STEP 1: Load project with creator info
            Project project = await projectRepo.GetFirstBySearch(
                p => p.TenantId == request.TenantId && p.Id == request.ProjectId,
                cancellationToken,
                include => include.Include(p => p.CreatedBy)
                                 .ThenInclude(cb => cb.User))
                ?? throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());

            // STEP 2: Determine user's role in tenant and project
            TenantCtxSnapshot? tenantSnapshot = await currentUser.GetTenantSnapshotAsync(request.TenantId, cancellationToken);
            bool isSuperAdmin = currentUser.IsSuperAdmin;
            bool isTenantAdmin = tenantSnapshot?.IsTenantAdmin ?? false;

            // STEP 3: Members count + current user's membership in two targeted queries (no full member load)
            int membersCount = await projectMemberRepo.CountAsync(
                pm => pm.ProjectId == request.ProjectId && pm.TenantId == request.TenantId,
                cancellationToken);

            ProjectMember? projectMembership = await projectMemberRepo.GetFirstBySearch(
                pm => pm.ProjectId == request.ProjectId
                    && pm.TenantId == request.TenantId
                    && pm.UserId == currentUser.Id,
                include => include.Include(pm => pm.MemberRole));

            // STEP 4: Determine user role code hierarchy
            // 1. Project membership role (if exists)
            // 2. Tenant Admin (if no project membership)
            // 3. System SuperAdmin (if no project membership or tenant admin)
            string userRoleCode;
            if (projectMembership is not null)
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

            // STEP 5: Get user's permissions for this project
            HashSet<string> userPermissions = new HashSet<string>();
            ProjectCtxSnapshot? projectSnapshot = await currentUser.GetProjectSnapshotAsync(request.ProjectId, cancellationToken);
            if (projectSnapshot is not null)
            {
                userPermissions = projectSnapshot.ProjectPermissionCodes;
            }

            // STEP 6: Get project currency
            ProjectCurrency? currency = await currencyRepo.GetFirstBySearch(
                x => x.ProjectId == request.ProjectId,
                cancellationToken);

            return new ProjectDetailsWeb
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
                UserRoleCode = userRoleCode,
                MembersCount = membersCount,
                UserPermissions = userPermissions,
                Currency = currency is null
                    ? null
                    : new ProjectCurrencyWeb { Code = currency.Code, Name = currency.Name, Symbol = currency.Symbol }
            };
        }
    }
}
