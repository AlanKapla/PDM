using Business.Interfaces.Exceptions;
using Entities.Models.Projects;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.ReorderProjectCostCategories
{
    public sealed class ReorderProjectCostCategoriesCommandHandler
        : IRequestHandler<ReorderProjectCostCategoriesCommand, Unit>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectCostCategory> categoryRepo;

        public ReorderProjectCostCategoriesCommandHandler(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectCostCategory> categoryRepo)
        {
            this.projectRepo = projectRepo;
            this.categoryRepo = categoryRepo;
        }

        public async Task<Unit> Handle(ReorderProjectCostCategoriesCommand request, CancellationToken cancellationToken)
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

            Dictionary<Guid, ProjectCostCategory> categoryDict = categories.ToDictionary(c => c.Id);

            for (int i = 0; i < request.CategoryIds.Count; i++)
            {
                Guid categoryId = request.CategoryIds[i];
                if (categoryDict.TryGetValue(categoryId, out ProjectCostCategory? category))
                {
                    category.Order = i + 1;
                    await categoryRepo.Update(category);
                }
            }

            await categoryRepo.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
