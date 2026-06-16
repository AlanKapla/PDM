using Business.Interfaces.Exceptions;
using Entities.Models.Projects;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.DeleteProjectUnit
{
    public sealed class DeleteProjectUnitCommandHandler : IRequestHandler<DeleteProjectUnitCommand, Unit>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectUnit> projectUnitRepo;

        public DeleteProjectUnitCommandHandler(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectUnit> projectUnitRepo)
        {
            this.projectRepo = projectRepo;
            this.projectUnitRepo = projectUnitRepo;
        }

        public async Task<Unit> Handle(DeleteProjectUnitCommand request, CancellationToken cancellationToken)
        {
            bool projectExists = await projectRepo.AnyAsync(
                p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
                cancellationToken);

            if (!projectExists)
            {
                throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());
            }

            ProjectUnit? unit = await projectUnitRepo.GetFirstBySearch(
                u => u.Id == request.UnitId && u.ProjectId == request.ProjectId);

            if (unit is null)
            {
                throw new NotFoundApiException(nameof(ProjectUnit), request.UnitId.ToString());
            }

            await projectUnitRepo.Delete(unit);
            await projectUnitRepo.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
