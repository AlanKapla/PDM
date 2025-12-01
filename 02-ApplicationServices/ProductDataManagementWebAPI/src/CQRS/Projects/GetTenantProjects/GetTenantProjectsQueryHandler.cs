using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.GetTenantProjects
{
    public class GetTenantProjectsQueryHandler : IRequestHandler<GetTenantProjectsQuery, IEnumerable<ProjectDetailsWeb>>
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
            var userProjectMembers = await projectMemberRepo.GetBySearch(
                pm => pm.TenantId == request.TenantId && pm.UserId == currentUser.Id,
                include => include.Include(pm => pm.Project)
                                 .ThenInclude(p => p.CreatedBy)
                                 .ThenInclude(cb => cb.User));

            var result = new List<ProjectDetailsWeb>();

            foreach (var projectMember in userProjectMembers)
            {
                var project = projectMember.Project;
                
                var membersCount = await projectMemberRepo.GetBySearch(
                    pm => pm.ProjectId == project.Id);

                var projectWeb = new ProjectDetailsWeb(
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

                result.Add(projectWeb);
            }

            return result.OrderByDescending(p => p.CreatedAt);
        }
    }
}