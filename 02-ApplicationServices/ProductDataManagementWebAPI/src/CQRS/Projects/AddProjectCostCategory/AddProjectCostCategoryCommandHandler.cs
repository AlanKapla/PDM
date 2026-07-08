using Business.Interfaces.Exceptions;
using Entities.Models.Projects;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.AddProjectCostCategory
{
    public sealed class AddProjectCostCategoryCommandHandler : IRequestHandler<AddProjectCostCategoryCommand, Guid>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectCostCategory> categoryRepo;

        public AddProjectCostCategoryCommandHandler(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectCostCategory> categoryRepo)
        {
            this.projectRepo = projectRepo;
            this.categoryRepo = categoryRepo;
        }

        public async Task<Guid> Handle(AddProjectCostCategoryCommand request, CancellationToken cancellationToken)
        {
            bool projectExists = await projectRepo.AnyAsync(
                p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
                cancellationToken);

            if (!projectExists)
            {
                throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());
            }

            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                bool codeExists = await categoryRepo.AnyAsync(
                    c => c.ProjectId == request.ProjectId && c.Code == request.Code,
                    cancellationToken);

                if (codeExists)
                {
                    throw new ConflictApiException(
                        nameof(ProjectCostCategory),
                        request.Code!,
                        "A category with this code already exists in the project.");
                }
            }

            int maxOrder = await GetMaxOrderAsync(request.ProjectId, cancellationToken);

            ProjectCostCategory category = new ProjectCostCategory
            {
                ProjectId = request.ProjectId,
                Name = request.Name,
                Code = request.Code,
                Color = request.Color,
                Order = maxOrder + 1
            };

            await categoryRepo.Insert(category);
            await categoryRepo.SaveChangesAsync(cancellationToken);

            return category.Id;
        }

        private async Task<int> GetMaxOrderAsync(Guid projectId, CancellationToken cancellationToken)
        {
            IEnumerable<ProjectCostCategory> existing = await categoryRepo.GetBySearch(
                c => c.ProjectId == projectId);

            return existing.Any() ? existing.Max(c => c.Order) : 0;
        }
    }
}
