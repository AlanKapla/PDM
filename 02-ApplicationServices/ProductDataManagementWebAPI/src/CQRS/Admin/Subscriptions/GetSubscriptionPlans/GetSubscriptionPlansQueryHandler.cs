using Business.Interfaces.WebModels.Admin;
using Entities.Models.Subscriptions;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Admin.Subscriptions.GetSubscriptionPlans;

public sealed class GetSubscriptionPlansQueryHandler
    : IRequestHandler<GetSubscriptionPlansQuery, IEnumerable<SubscriptionPlanDefinitionWeb>>
{
    private readonly IReadRepository<SubscriptionPlanDefinition> planRepo;

    public GetSubscriptionPlansQueryHandler(IReadRepository<SubscriptionPlanDefinition> planRepo)
    {
        this.planRepo = planRepo;
    }

    public async Task<IEnumerable<SubscriptionPlanDefinitionWeb>> Handle(
        GetSubscriptionPlansQuery request,
        CancellationToken cancellationToken)
    {
        IEnumerable<SubscriptionPlanDefinition> plans = await planRepo.GetAll();

        return plans
            .OrderBy(p => (int)p.Plan)
            .Select(p => new SubscriptionPlanDefinitionWeb(
                p.Id,
                p.Plan.ToString(),
                p.Name,
                p.MaxProjects,
                p.MaxUsers,
                p.Price,
                p.Currency,
                p.IsActive));
    }
}
