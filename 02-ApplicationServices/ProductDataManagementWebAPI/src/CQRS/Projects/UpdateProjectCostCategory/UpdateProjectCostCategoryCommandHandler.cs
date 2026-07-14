using Business.Interfaces.Exceptions;
using Entities.Models.Projects;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.UpdateProjectCostCategory
{
    public sealed class UpdateProjectCostCategoryCommandHandler
        : IRequestHandler<UpdateProjectCostCategoryCommand, MediatR.Unit>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectCostCategory> categoryRepo;

        public UpdateProjectCostCategoryCommandHandler(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectCostCategory> categoryRepo)
        {
            this.projectRepo = projectRepo;
            this.categoryRepo = categoryRepo;
        }

        public async Task<Unit> Handle(UpdateProjectCostCategoryCommand request, CancellationToken cancellationToken)
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

            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                bool codeExists = await categoryRepo.AnyAsync(
                    c => c.ProjectId == request.ProjectId
                         && c.Code == request.Code
                         && c.Id != request.CategoryId,
                    cancellationToken);

                if (codeExists)
                {
                    throw new ConflictApiException(
                        nameof(ProjectCostCategory),
                        request.Code!,
                        "A category with this code already exists in the project.");
                }
            }

            category.Name = request.Name;
            category.Code = request.Code;
            category.Order = request.Order;
            category.Color = request.Color;

            await categoryRepo.Update(category);
            await categoryRepo.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
