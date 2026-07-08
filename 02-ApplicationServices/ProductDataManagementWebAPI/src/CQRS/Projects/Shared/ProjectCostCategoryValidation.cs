using Business.Interfaces.Exceptions;
using Entities.Models.Projects;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.Shared
{
    public static class ProjectCostCategoryValidation
    {
        public static async Task ValidateCategoryBelongsToProjectAsync(
            Guid? categoryId,
            Guid projectId,
            IReadRepository<ProjectCostCategory> categoryRepo,
            CancellationToken cancellationToken)
        {
            if (!categoryId.HasValue)
            {
                return;
            }

            bool exists = await categoryRepo.AnyAsync(
                c => c.Id == categoryId.Value && c.ProjectId == projectId,
                cancellationToken);

            if (!exists)
            {
                throw new NotFoundApiException(nameof(ProjectCostCategory), categoryId.Value.ToString());
            }
        }
    }
}
