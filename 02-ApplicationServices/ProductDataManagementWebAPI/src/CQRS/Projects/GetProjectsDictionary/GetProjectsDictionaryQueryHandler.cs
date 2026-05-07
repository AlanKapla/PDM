using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using CQRS.Extensions;
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

namespace CQRS.Projects.GetProjectsDictionary
{
    public class GetProjectsDictionaryQueryHandler : IRequestHandler<GetProjectsDictionaryQuery, Dictionary<Guid, string>>
    {
        private readonly IRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly ICurrentUser currentUser;

        public GetProjectsDictionaryQueryHandler(
            IRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            ICurrentUser currentUser)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.currentUser = currentUser;
        }

        public async Task<Dictionary<Guid, string>> Handle(GetProjectsDictionaryQuery request, CancellationToken cancellationToken)
        {
            Guid tenantId = request.TenantId;

            // Check if user is tenant admin
            var tenantSnapshot = await currentUser.GetActiveTenantSnapshotAsync(cancellationToken);
            bool isTenantAdmin = tenantSnapshot?.IsTenantAdmin ?? false;

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
                        && (pm.MemberRole!.Code == RoleCodes.ProjectEditor || pm.MemberRole.Code == RoleCodes.ProjectAdmin),
                    q => q.Include(pm => pm.Project).Include(pm => pm.MemberRole));

                var membershipsList = userProjectMemberships.ToList();

                // Filtruj projekty według roli:
                // - Admin projektu widzi projekt (aktywny lub nieaktywny)
                // - Editor widzi tylko aktywny projekt
                projects = membershipsList
                    .Where(pm => pm.MemberRole?.Code.IsProjectAdmin() == true || pm.Project.IsActive)
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
