using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Entities.Models.Projects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.GetProjectsDictionary
{
    public sealed class GetProjectsDictionaryQueryHandler : IRequestHandler<GetProjectsDictionaryQuery, Dictionary<Guid, string>>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly ICurrentUser currentUser;

        public GetProjectsDictionaryQueryHandler(
            IReadRepository<Project> projectRepo,
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
            TenantCtxSnapshot? tenantSnapshot = await currentUser.GetActiveTenantSnapshotAsync(cancellationToken);
            bool isTenantAdmin = tenantSnapshot?.IsAdmin ?? false;

            IEnumerable<Project> projects;

            if (isTenantAdmin)
            {
                // Tenant admin sees all projects (active and inactive).
                projects = await projectRepo.GetBySearch(
                    p => p.TenantId == tenantId);
            }
            else
            {
                // Non-admin members: admin sees all projects, others see only active
                IEnumerable<ProjectMember> userProjectMemberships = await projectMemberRepo.GetBySearch(
                    pm => pm.TenantId == tenantId
                        && pm.UserId == currentUser.Id
                        && pm.IsActive
                        && (pm.IsAdmin || pm.Project.IsActive),
                    q => q.Include(pm => pm.Project));

                projects = userProjectMemberships
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
