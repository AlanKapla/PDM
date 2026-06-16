using Business.Interfaces.Exceptions;
using Entities.Models.Projects;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.ReorderProjectUnits
{
    public sealed class ReorderProjectUnitsCommandHandler : IRequestHandler<ReorderProjectUnitsCommand, Unit>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectUnit> projectUnitRepo;

        public ReorderProjectUnitsCommandHandler(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectUnit> projectUnitRepo)
        {
            this.projectRepo = projectRepo;
            this.projectUnitRepo = projectUnitRepo;
        }

        public async Task<Unit> Handle(ReorderProjectUnitsCommand request, CancellationToken cancellationToken)
        {
            bool projectExists = await projectRepo.AnyAsync(
                p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
                cancellationToken);

            if (!projectExists)
            {
                throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());
            }

            IEnumerable<ProjectUnit> units = await projectUnitRepo.GetBySearch(
                u => u.ProjectId == request.ProjectId);

            Dictionary<Guid, ProjectUnit> unitDict = units.ToDictionary(u => u.Id);

            for (int i = 0; i < request.UnitIds.Count; i++)
            {
                Guid unitId = request.UnitIds[i];
                if (unitDict.TryGetValue(unitId, out ProjectUnit? unit))
                {
                    unit.Order = i + 1;
                    await projectUnitRepo.Update(unit);
                }
            }

            await projectUnitRepo.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
