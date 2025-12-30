using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Enums;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.GetProjectsDictionary
{
    public class GetProjectsDictionaryQueryHandler : IRequestHandler<GetProjectsDictionaryQuery, Dictionary<Guid, string>>
    {
        private readonly IRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly IAccessService accessService;
        private readonly ICurrentUser currentUser;

        public GetProjectsDictionaryQueryHandler(
            IRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            IAccessService accessService,
            ICurrentUser currentUser)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.accessService = accessService;
            this.currentUser = currentUser;
        }

        public async Task<Dictionary<Guid, string>> Handle(GetProjectsDictionaryQuery request, CancellationToken cancellationToken)
        {
            Guid tenantId = request.TenantId;

            bool isTenantAdmin = await accessService.IsTenantAdminAsync(tenantId, cancellationToken);

            IEnumerable<Project> projects;

            if (isTenantAdmin)
            {
                // Admin tenanta widzi wszystkie projekty (aktywne i nieaktywne)
                projects = await projectRepo.GetBySearch(
                    p => p.TenantId == tenantId);
            }
            else
            {
                // Pobierz wszystkie membership użytkownika w tym tenancie
                var userProjectMemberships = await projectMemberRepo.GetBySearch(
                    pm => pm.TenantId == tenantId 
                        && pm.UserId == currentUser.Id
                        && (pm.Role == ProjectRole.Editor || pm.Role == ProjectRole.Admin),
                    q => q.Include(pm => pm.Project));

                var membershipsList = userProjectMemberships.ToList();

                // Filtruj projekty według roli:
                // - Admin projektu widzi projekt (aktywny lub nieaktywny)
                // - Editor widzi tylko aktywny projekt
                projects = membershipsList
                    .Where(pm => pm.Role == ProjectRole.Admin || pm.Project.IsActive)
                    .Select(pm => pm.Project)
                    .Distinct()
                    .ToList();
            }

            Dictionary<Guid, string> result = projects.ToDictionary(
                p => p.Id,
                p => p.IsActive ? p.Name : $"{p.Name} [Nieaktywny]"
            );

            return result;
        }
    }
}
