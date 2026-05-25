using Business.Interfaces.WebModels.Admin;
using Entities.Models.Subscriptions;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Admin.Subscriptions.GetAllPlanDefinitions;

public sealed class GetAllPlanDefinitionsQueryHandler
    : IRequestHandler<GetAllPlanDefinitionsQuery, IEnumerable<AdminSubscriptionPlanWeb>>
{
    private readonly IReadRepository<SubscriptionPlanDefinition> planRepo;

    public GetAllPlanDefinitionsQueryHandler(IReadRepository<SubscriptionPlanDefinition> planRepo)
    {
        this.planRepo = planRepo;
    }

    public async Task<IEnumerable<AdminSubscriptionPlanWeb>> Handle(
        GetAllPlanDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        IEnumerable<SubscriptionPlanDefinition> plans = await planRepo.GetAll();

        return plans
            .OrderBy(p => (int)p.Plan)
            .Select(p => new AdminSubscriptionPlanWeb(
                p.Id,
                (int)p.Plan,
                p.Name,
                p.MaxProjects,
                p.MaxUsers,
                p.Price,
                p.Currency,
                p.IsActive,
                p.UpdatedAt));
    }
}
