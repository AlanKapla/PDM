using Business.Interfaces.WebModels.Projects;
using Business.Interfaces.Model;
using Entities.Models;
using Entities.Enums;
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
            // Pobierz projekty użytkownika z filtrowaniem w bazie danych
            // Admin projektu widzi wszystkie projekty, member tylko aktywne
            var userProjectMembers = await projectMemberRepo.GetBySearch(
                pm => pm.TenantId == request.TenantId 
                    && pm.UserId == currentUser.Id
                    && (pm.Role == ProjectRole.Admin || pm.Project.IsActive),
                include => include.Include(pm => pm.Project)
                                 .ThenInclude(p => p.CreatedBy)
                                 .ThenInclude(cb => cb.User));

            // Pobierz wszystkich członków dla tych projektów w jednym zapytaniu
            var projectIds = userProjectMembers.Select(pm => pm.ProjectId).ToList();
            
            var allProjectMembers = await projectMemberRepo.GetBySearch(
                pm => projectIds.Contains(pm.ProjectId));

            // Zbuduj słownik z liczbą członków dla każdego projektu
            var membersCountDict = allProjectMembers
                .GroupBy(pm => pm.ProjectId)
                .ToDictionary(g => g.Key, g => g.Count());

            var result = userProjectMembers
                .Select(projectMember =>
                {
                    var project = projectMember.Project;
                    int membersCount = membersCountDict.TryGetValue(project.Id, out int count) ? count : 0;

                    return new ProjectDetailsWeb(
                        Id: project.Id,
                        TenantId: project.TenantId,
                        Name: project.Name,
                        IsActive: project.IsActive,
                        CreatedAt: project.CreatedAt,
                        CreatedByUserId: project.CreatedByUserId,
                        CreatedByUserName: $"{project.CreatedBy?.User?.FirstName} {project.CreatedBy?.User?.LastName}".Trim(),
                        UserRole: projectMember.Role,
                        MembersCount: membersCount
                    );
                })
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            return result;
        }
    }
}
