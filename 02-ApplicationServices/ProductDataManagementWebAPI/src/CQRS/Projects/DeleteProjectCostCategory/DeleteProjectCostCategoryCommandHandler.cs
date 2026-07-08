using Business.Interfaces.Exceptions;
using Entities.Models.Costs;
using Entities.Models.CostTrackers;
using Entities.Models.Projects;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.DeleteProjectCostCategory
{
    public sealed class DeleteProjectCostCategoryCommandHandler
        : IRequestHandler<DeleteProjectCostCategoryCommand, MediatR.Unit>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectCostCategory> categoryRepo;
        private readonly IRepository<ProjectCost> projectCostRepo;
        private readonly IRepository<TrackedCost> trackedCostRepo;

        public DeleteProjectCostCategoryCommandHandler(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectCostCategory> categoryRepo,
            IRepository<ProjectCost> projectCostRepo,
            IRepository<TrackedCost> trackedCostRepo)
        {
            this.projectRepo = projectRepo;
            this.categoryRepo = categoryRepo;
            this.projectCostRepo = projectCostRepo;
            this.trackedCostRepo = trackedCostRepo;
        }

        public async Task<MediatR.Unit> Handle(DeleteProjectCostCategoryCommand request, CancellationToken cancellationToken)
        {
            bool projectExists = await projectRepo.AnyAsync(
                p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
                cancellationToken);

            if (!projectExists)
            {
                throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());
            }

            ProjectCostCategory? category = await categoryRepo.GetFirstBySearch(
                c => c.Id == request.CategoryId && c.ProjectId == request.ProjectId);

            if (category is null)
            {
                throw new NotFoundApiException(nameof(ProjectCostCategory), request.CategoryId.ToString());
            }

            await DetachCategoryFromCostsAsync(request.CategoryId, request.ProjectId, cancellationToken);

            await categoryRepo.Delete(category);
            await categoryRepo.SaveChangesAsync(cancellationToken);

            return MediatR.Unit.Value;
        }

        private async Task DetachCategoryFromCostsAsync(
            Guid categoryId,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            IEnumerable<ProjectCost> projectCosts = await projectCostRepo.GetBySearch(
                c => c.ProjectId == projectId && c.CategoryId == categoryId);

            foreach (ProjectCost cost in projectCosts)
            {
                cost.CategoryId = null;
                await projectCostRepo.Update(cost);
            }

            IEnumerable<TrackedCost> trackedCosts = await trackedCostRepo.GetBySearch(
                c => c.ProjectId == projectId && c.CategoryId == categoryId);

            foreach (TrackedCost cost in trackedCosts)
            {
                cost.CategoryId = null;
                await trackedCostRepo.Update(cost);
            }

            if (projectCosts.Any() || trackedCosts.Any())
            {
                await projectCostRepo.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
