using Business.Interfaces.Exceptions;
using Entities.Models.Projects;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.AddProjectUnit
{
    public sealed class AddProjectUnitCommandHandler : IRequestHandler<AddProjectUnitCommand, Guid>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectUnit> projectUnitRepo;

        public AddProjectUnitCommandHandler(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectUnit> projectUnitRepo)
        {
            this.projectRepo = projectRepo;
            this.projectUnitRepo = projectUnitRepo;
        }

        public async Task<Guid> Handle(AddProjectUnitCommand request, CancellationToken cancellationToken)
        {
            bool projectExists = await projectRepo.AnyAsync(
                p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
                cancellationToken);

            if (!projectExists)
            {
                throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());
            }

            int maxOrder = await GetMaxOrderAsync(request.ProjectId, cancellationToken);

            ProjectUnit unit = new ProjectUnit
            {
                ProjectId = request.ProjectId,
                Code = request.Code,
                Name = request.Name,
                Symbol = request.Symbol,
                Order = maxOrder + 1
            };

            await projectUnitRepo.Insert(unit);
            await projectUnitRepo.SaveChangesAsync(cancellationToken);

            return unit.Id;
        }

        private async Task<int> GetMaxOrderAsync(Guid projectId, CancellationToken cancellationToken)
        {
            IEnumerable<ProjectUnit> existing = await projectUnitRepo.GetBySearch(
                u => u.ProjectId == projectId);

            return existing.Any() ? existing.Max(u => u.Order) : 0;
        }
    }
}
