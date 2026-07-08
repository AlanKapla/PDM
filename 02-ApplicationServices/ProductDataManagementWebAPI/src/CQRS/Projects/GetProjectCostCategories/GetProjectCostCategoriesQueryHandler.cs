using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.Projects;
using Entities.Models.Projects;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.GetProjectCostCategories
{
    public sealed class GetProjectCostCategoriesQueryHandler
        : IRequestHandler<GetProjectCostCategoriesQuery, List<ProjectCostCategoryWeb>>
    {
        private readonly IReadRepository<ProjectCostCategory> categoryRepo;
        private readonly IReadRepository<Project> projectRepo;

        public GetProjectCostCategoriesQueryHandler(
            IReadRepository<ProjectCostCategory> categoryRepo,
            IReadRepository<Project> projectRepo)
        {
            this.categoryRepo = categoryRepo;
            this.projectRepo = projectRepo;
        }

        public async Task<List<ProjectCostCategoryWeb>> Handle(
            GetProjectCostCategoriesQuery request,
            CancellationToken cancellationToken)
        {
            bool projectExists = await projectRepo.AnyAsync(
                p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
                cancellationToken);

            if (!projectExists)
            {
                throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());
            }

            IEnumerable<ProjectCostCategory> categories = await categoryRepo.GetBySearch(
                c => c.ProjectId == request.ProjectId);

            return categories
                .OrderBy(c => c.Order)
                .Select(MapToWeb)
                .ToList();
        }

        private static ProjectCostCategoryWeb MapToWeb(ProjectCostCategory category) =>
            new()
            {
                Id = category.Id,
                Name = category.Name,
                Code = category.Code,
                Order = category.Order,
                Color = category.Color
            };
    }
}
