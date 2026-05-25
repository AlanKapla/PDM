using Business.Interfaces.WebModels.Subscriptions;
using Entities.Models.Subscriptions;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Subscriptions.GetPublicSubscriptionPlans;

public sealed class GetPublicSubscriptionPlansQueryHandler
    : IRequestHandler<GetPublicSubscriptionPlansQuery, IEnumerable<SubscriptionPlanInfoWeb>>
{
    private readonly IReadRepository<SubscriptionPlanDefinition> planRepo;

    public GetPublicSubscriptionPlansQueryHandler(IReadRepository<SubscriptionPlanDefinition> planRepo)
    {
        this.planRepo = planRepo;
    }

    public async Task<IEnumerable<SubscriptionPlanInfoWeb>> Handle(
        GetPublicSubscriptionPlansQuery request,
        CancellationToken cancellationToken)
    {
        IEnumerable<SubscriptionPlanDefinition> plans = await planRepo.GetAll();

        return plans
            .Where(p => p.IsActive)
            .OrderBy(p => (int)p.Plan)
            .Select(MapToWeb);
    }

    private static SubscriptionPlanInfoWeb MapToWeb(SubscriptionPlanDefinition plan) =>
        new(
            (int)plan.Plan,
            plan.Name,
            plan.MaxProjects,
            plan.MaxUsers,
            plan.Price,
            plan.Currency);
}
