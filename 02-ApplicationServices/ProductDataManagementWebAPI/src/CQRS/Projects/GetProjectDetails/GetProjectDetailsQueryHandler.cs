using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;
using Entities.Enums;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.GetProjectDetails
{
    public class GetProjectDetailsQueryHandler : IRequestHandler<GetProjectDetailsQuery, ProjectDetailsWeb?>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly ICurrentUser currentUser;

        public GetProjectDetailsQueryHandler(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.currentUser = currentUser;
        }

        public async Task<ProjectDetailsWeb?> Handle(GetProjectDetailsQuery request, CancellationToken cancellationToken)
        {
            // Wszystkie walidacje wykonane w validatorze
            IEnumerable<ProjectMember> projectMembers = await projectMemberRepo.GetBySearch(
                pm => pm.TenantId == request.TenantId 
                    && pm.ProjectId == request.ProjectId 
                    && pm.UserId == currentUser.Id
                    && (pm.Role == ProjectRole.Admin || pm.Project.IsActive),
                include => include.Include(pm => pm.Project)
                                 .ThenInclude(p => p.CreatedBy)
                                 .ThenInclude(cb => cb.User)
            );

            ProjectMember projectMember = projectMembers.First();
            Project project = projectMember.Project;

            IEnumerable<ProjectMember> membersCount = await projectMemberRepo.GetBySearch(
                pm => pm.ProjectId == project.Id);

            ProjectDetailsWeb result = new(
                Id: project.Id,
                TenantId: project.TenantId,
                Name: project.Name,
                IsActive: project.IsActive,
                CreatedAt: project.CreatedAt,
                CreatedByUserId: project.CreatedByUserId,
                CreatedByUserName: $"{project.CreatedBy?.User?.FirstName} {project.CreatedBy?.User?.LastName}".Trim(),
                UserRole: projectMember.Role,
                MembersCount: membersCount.Count()
            );

            return result;
        }
    }
}
