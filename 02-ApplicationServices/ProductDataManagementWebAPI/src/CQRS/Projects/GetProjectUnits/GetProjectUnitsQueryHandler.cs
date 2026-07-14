using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.Projects;
using Entities.Models.Projects;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.GetProjectUnits
{
    public sealed class GetProjectUnitsQueryHandler : IRequestHandler<GetProjectUnitsQuery, List<ProjectUnitWeb>>
    {
        private readonly IReadRepository<ProjectUnit> projectUnitRepo;
        private readonly IReadRepository<Project> projectRepo;

        public GetProjectUnitsQueryHandler(
            IReadRepository<ProjectUnit> projectUnitRepo,
            IReadRepository<Project> projectRepo)
        {
            this.projectUnitRepo = projectUnitRepo;
            this.projectRepo = projectRepo;
        }

        public async Task<List<ProjectUnitWeb>> Handle(GetProjectUnitsQuery request, CancellationToken cancellationToken)
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

            return units
                .OrderBy(u => u.Order)
                .Select(u => new ProjectUnitWeb
                {
                    Id = u.Id,
                    Code = u.Code,
                    Name = u.Name,
                    Symbol = u.Symbol,
                    Order = u.Order
                })
                .ToList();
        }
    }
}
