using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Projects;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using MediatR;
using Repositories.Repository.Interfaces;
using System.Linq;

namespace CQRS.Projects.CreateProject
{
    public sealed class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ProjectDetailsWeb>
    {
        private static readonly (string Code, string Name, string? Symbol)[] DefaultUnits = new[]
        {
            ("szt",  "Sztuka",               (string?)null),
            ("m",    "Metr",                 (string?)null),
            ("m²",   "Metr kwadratowy",      (string?)"m²"),
            ("m³",   "Metr sześcienny",      (string?)"m³"),
            ("kg",   "Kilogram",             (string?)null),
            ("mb",   "Metr bieżący",         (string?)null),
            ("godz", "Godzina",              (string?)null),
            ("kpl",  "Komplet",              (string?)null),
            ("t",    "Tona",                 (string?)null),
            ("km",   "Kilometr",             (string?)null),
            ("l",    "Litr",                 (string?)null),
            ("opak", "Opakowanie",           (string?)null),
            ("r-g",  "Roboczogodzina",       (string?)null),
        };

        private static readonly (string Code, string Name)[] DefaultCostCategories = new[]
        {
            ("mat", "Materiały budowlane"),
            ("rob", "Robocizna"),
            ("sprzet", "Sprzęt i maszyny"),
            ("transport", "Transport i logistyka"),
            ("uslugi", "Usługi zewnętrzne"),
            ("admin", "Administracja i biuro"),
            ("media", "Energia i media"),
            ("podwyk", "Podwykonawcy"),
            ("narz", "Narzędzia i wyposażenie"),
            ("inne", "Inne"),
        };

        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectMember> projectMemberRepo;
        private readonly IRepository<ProjectCurrency> currencyRepo;
        private readonly IRepository<ProjectUnit> projectUnitRepo;
        private readonly IRepository<ProjectCostCategory> projectCostCategoryRepo;
        private readonly IPermissionsVersionService permissionsVersionService;
        private readonly ICurrentUser currentUser;

        public CreateProjectCommandHandler(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectMember> projectMemberRepo,
            IRepository<ProjectCurrency> currencyRepo,
            IRepository<ProjectUnit> projectUnitRepo,
            IRepository<ProjectCostCategory> projectCostCategoryRepo,
            IPermissionsVersionService permissionsVersionService,
            ICurrentUser currentUser)
        {
            this.projectRepo = projectRepo;
            this.projectMemberRepo = projectMemberRepo;
            this.currencyRepo = currencyRepo;
            this.projectUnitRepo = projectUnitRepo;
            this.projectCostCategoryRepo = projectCostCategoryRepo;
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

            int unitOrder = 1;
            foreach ((string code, string name, string? symbol) in DefaultUnits)
            {
                await projectUnitRepo.Insert(new ProjectUnit
                {
                    ProjectId = project.Id,
                    Code = code,
                    Name = name,
                    Symbol = symbol,
                    Order = unitOrder++
                });
            }
            await projectUnitRepo.SaveChangesAsync(cancellationToken);

            int categoryOrder = 1;
            foreach ((string code, string name) in DefaultCostCategories)
            {
                await projectCostCategoryRepo.Insert(new ProjectCostCategory
                {
                    ProjectId = project.Id,
                    Code = code,
                    Name = name,
                    Order = categoryOrder++
                });
            }
            await projectCostCategoryRepo.SaveChangesAsync(cancellationToken);

            ProjectMember projectMember = new ProjectMember
            {
                TenantId = tenantId,
                ProjectId = project.Id,
                UserId = currentUser.Id,
                IsAdmin = true,
                IsActive = true,
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
                IsAdmin = true,
                CanViewAllResources = true,
                MembersCount = 1,
                UserPermissions = userPermissions,
                Currency = new ProjectCurrencyWeb { Code = "PLN", Name = "Polski złoty", Symbol = "zł" }
            };
        }
    }
}
