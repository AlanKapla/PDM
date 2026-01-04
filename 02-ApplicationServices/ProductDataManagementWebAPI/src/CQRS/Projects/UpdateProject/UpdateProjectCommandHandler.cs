using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.UpdateProject
{
    public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, ProjectDetailsWeb>
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

            // Get current user's project membership with role
            ProjectMember? projectMember = await projectMemberRepo.GetFirstBySearch(
                pm => pm.ProjectId == project.Id 
                    && pm.UserId == currentUser.Id,
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
                pm => pm.ProjectId == project.Id);

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
                UserRoleCode: projectMember?.MemberRole?.Code ?? RoleCodes.ProjectMember,
                MembersCount: allMembers.Count()
            );
        }
    }
}
