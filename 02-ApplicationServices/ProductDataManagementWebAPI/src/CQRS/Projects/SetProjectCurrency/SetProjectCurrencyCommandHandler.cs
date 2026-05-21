using Business.Interfaces.Exceptions;
using Entities.Models.Projects;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.SetProjectCurrency
{
    public sealed class SetProjectCurrencyCommandHandler : IRequestHandler<SetProjectCurrencyCommand, Unit>
    {
        private readonly IReadRepository<Project> projectRepo;
        private readonly IRepository<ProjectCurrency> currencyRepo;

        public SetProjectCurrencyCommandHandler(
            IReadRepository<Project> projectRepo,
            IRepository<ProjectCurrency> currencyRepo)
        {
            this.projectRepo = projectRepo;
            this.currencyRepo = currencyRepo;
        }

        public async Task<Unit> Handle(SetProjectCurrencyCommand request, CancellationToken cancellationToken)
        {
            bool projectExists = await projectRepo.AnyAsync(
                p => p.Id == request.ProjectId && p.TenantId == request.TenantId,
                cancellationToken);

            if (!projectExists)
            {
                throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());
            }

            ProjectCurrency? existing = await currencyRepo.GetFirstBySearch(
                x => x.ProjectId == request.ProjectId);

            if (existing is not null)
            {
                existing.Code = request.Code;
                existing.Name = request.Name;
                existing.Symbol = request.Symbol;
                await currencyRepo.Update(existing);
            }
            else
            {
                ProjectCurrency currency = new ProjectCurrency
                {
                    ProjectId = request.ProjectId,
                    Code = request.Code,
                    Name = request.Name,
                    Symbol = request.Symbol
                };
                await currencyRepo.Insert(currency);
            }

            await currencyRepo.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
