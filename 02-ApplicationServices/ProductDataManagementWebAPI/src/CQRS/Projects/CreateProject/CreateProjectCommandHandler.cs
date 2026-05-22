using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Projects;
using Entities.Enums;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.CreateProject
{
    public sealed class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ProjectDetailsWeb>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly IReadRepository<Role> roleRepo;
        private readonly IRepository<ProjectCurrency> currencyRepo;
        private readonly IPermissionsVersionService permissionsVersionService;
        private readonly ICurrentUser currentUser;

        public CreateProjectCommandHandler(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            IReadRepository<Role> roleRepo,
            IRepository<ProjectCurrency> currencyRepo,
            IPermissionsVersionService permissionsVersionService,
            ICurrentUser currentUser)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.roleRepo = roleRepo;
            this.currencyRepo = currencyRepo;
            this.permissionsVersionService = permissionsVersionService;
            this.currentUser = currentUser;
        }

        public async Task<ProjectDetailsWeb> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
        {
            Guid tenantId = request.TenantId;

            Project project = new Project
            {
                TenantId = tenantId,
                Name = request.Name,
                CreatedByUserId = currentUser.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await projectRepo.Insert(project);
            await projectRepo.SaveChangesAsync(cancellationToken);

            ProjectCurrency defaultCurrency = new ProjectCurrency
            {
                ProjectId = project.Id,
                Code = "PLN",
                Name = "Polski złoty",
                Symbol = "zł"
            };
            await currencyRepo.Insert(defaultCurrency);
            await currencyRepo.SaveChangesAsync(cancellationToken);

            // Get PROJECT.ADMIN role
            Role adminRole = await roleRepo.GetFirstBySearch(
                r => r.Scope == RoleScope.Project && r.Code == RoleCodes.ProjectAdmin,
                cancellationToken)
                ?? throw new NotFoundApiException(nameof(Role), RoleCodes.ProjectAdmin);

            ProjectMember projectMember = new ProjectMember
            {
                TenantId = tenantId,
                ProjectId = project.Id,
                UserId = currentUser.Id,
                RoleId = adminRole.Id,
                JoinedAt = DateTime.UtcNow
            };

            await projectMemberRepo.Insert(projectMember);
            await projectMemberRepo.SaveChangesAsync(cancellationToken);

            // Bump permissions version
            await permissionsVersionService.BumpVersionAsync(currentUser.Id, cancellationToken);

            string createdByUserName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();

            // Get user's permissions for newly created project
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
                CreatedByUserName = createdByUserName,
                UserRoleCode = RoleCodes.ProjectAdmin,
                MembersCount = 1,
                UserPermissions = userPermissions,
                Currency = new ProjectCurrencyWeb { Code = "PLN", Name = "Polski złoty", Symbol = "zł" }
            };
        }
    }
}
