using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.UpdateProject
{
    public sealed class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, ProjectDetailsWeb>
    {
        private readonly IRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly ICurrentUser currentUser;

        public UpdateProjectCommandHandler(
            IRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            IRepository<TenantMember> tenantMemberRepo,
            ICurrentUser currentUser)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.currentUser = currentUser;
        }

        public async Task<ProjectDetailsWeb> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
        {
            Project project = await projectRepo.GetFirstBySearch(
                p => p.Id == request.ProjectId && p.TenantId == request.TenantId)
                ?? throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());

            project.Name = request.Name.Trim();
            await projectRepo.Update(project);

            // Current user's project membership
            ProjectMember? projectMember = await projectMemberRepo.GetFirstBySearch(
                pm => pm.ProjectId == project.Id
                    && pm.TenantId == request.TenantId
                    && pm.UserId == currentUser.Id,
                include => include.Include(pm => pm.ModulePermissions));

            // Creator info
            TenantMember? creatorMember = await tenantMemberRepo.GetFirstBySearch(
                tm => tm.TenantId == request.TenantId
                    && tm.UserId == project.CreatedByUserId,
                include => include.Include(tm => tm.User));

            // Members count
            int membersCount = await projectMemberRepo.CountAsync(
                pm => pm.ProjectId == project.Id && pm.TenantId == request.TenantId,
                cancellationToken);

            // User's permissions for this project
            HashSet<string> userPermissions = new HashSet<string>();
            ProjectCtxSnapshot? projectSnapshot = await currentUser.GetProjectSnapshotAsync(project.Id, cancellationToken);
            if (projectSnapshot is not null)
            {
                userPermissions = projectSnapshot.ProjectPermissionCodes;
            }

            return new ProjectDetailsWeb
            {
                Id = project.Id,
                TenantId = project.TenantId,
                Name = project.Name,
                IsActive = project.IsActive,
                CreatedAt = project.CreatedAt,
                CreatedByUserId = project.CreatedByUserId,
                CreatedByUserName = creatorMember?.User is not null
                    ? $"{creatorMember.User.FirstName} {creatorMember.User.LastName}".Trim()
                    : "Unknown",
                IsAdmin = projectMember is not null && projectMember.IsAdmin,
                CanViewAllResources = projectSnapshot?.IsProjectAdmin ?? false || currentUser.IsSuperAdmin,
                MembersCount = membersCount,
                UserPermissions = userPermissions,
                Currency = null
            };
        }
    }
}
