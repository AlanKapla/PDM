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
        private readonly ICurrentUser currentUser;

        public UpdateProjectCommandHandler(
            IRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.currentUser = currentUser;
        }

        public async Task<ProjectDetailsWeb> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
        {
            if (currentUser.ActiveTenantId != request.TenantId)
            {
                throw new ForbiddenApiException("Cannot update project from another tenant");
            }

            Project project = (await projectRepo.GetFirstBySearch(
                p => p.Id == request.ProjectId && p.TenantId == request.TenantId && p.IsActive))
                ?? throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());

            project.Name = request.Name.Trim();
            await projectRepo.Update(project);

            ProjectMember? projectMember = await projectMemberRepo.GetFirstBySearch(
                pm => pm.ProjectId == project.Id 
                    && pm.UserId == currentUser.Id,
                include => include.Include(pm => pm.Project)
                                 .ThenInclude(p => p.CreatedBy)
                                 .ThenInclude(cb => cb.User)
                                 .Include(pm => pm.MemberRole)
            );

            IEnumerable<ProjectMember> membersCount = await projectMemberRepo.GetBySearch(
                pm => pm.ProjectId == project.Id);

            return new ProjectDetailsWeb(
                Id: project.Id,
                TenantId: project.TenantId,
                Name: project.Name,
                IsActive: project.IsActive,
                CreatedAt: project.CreatedAt,
                CreatedByUserId: project.CreatedByUserId,
                CreatedByUserName: projectMember?.Project?.CreatedBy?.User != null 
                    ? $"{projectMember.Project.CreatedBy.User.FirstName} {projectMember.Project.CreatedBy.User.LastName}".Trim()
                    : "Unknown",
                UserRoleCode: projectMember?.MemberRole?.Code ?? RoleCodes.ProjectMember,
                MembersCount: membersCount.Count()
            );
        }
    }
}
