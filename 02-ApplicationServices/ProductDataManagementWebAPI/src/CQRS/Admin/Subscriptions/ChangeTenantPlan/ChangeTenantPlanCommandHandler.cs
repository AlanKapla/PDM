using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.Admin;
using Entities.Models.Subscriptions;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Admin.Subscriptions.ChangeTenantPlan;

public sealed class ChangeTenantPlanCommandHandler
    : IRequestHandler<ChangeTenantPlanCommand, TenantSubscriptionSummaryWeb>
{
    private readonly IReadRepository<TenantSubscription> subscriptionRepo;
    private readonly IReadRepository<SubscriptionPlanDefinition> planRepo;

    public ChangeTenantPlanCommandHandler(
        IReadRepository<TenantSubscription> subscriptionRepo,
        IReadRepository<SubscriptionPlanDefinition> planRepo)
    {
        this.subscriptionRepo = subscriptionRepo;
        this.planRepo         = planRepo;
    }

    public async Task<TenantSubscriptionSummaryWeb> Handle(
        ChangeTenantPlanCommand request,
        CancellationToken cancellationToken)
    {
        TenantSubscription? subscription = await subscriptionRepo.GetFirstBySearch(
            s => s.TenantId == request.TenantId,
            cancellationToken);

        if (subscription is null)
        {
            throw new NotFoundApiException(nameof(TenantSubscription), request.TenantId.ToString());
        }

        SubscriptionPlanDefinition? planDefinition = await planRepo.GetFirstBySearch(
            p => p.Plan == request.Plan,
            cancellationToken);

        if (planDefinition is null)
        {
            throw new NotFoundApiException(nameof(SubscriptionPlanDefinition), request.Plan.ToString());
        }

        subscription.ApplyPlan(planDefinition);

        await subscriptionRepo.Update(subscription);
        await subscriptionRepo.SaveChangesAsync(cancellationToken);

        return new TenantSubscriptionSummaryWeb(
            subscription.TenantId,
            (int)subscription.Plan,
            (int)subscription.Status,
            subscription.MaxProjects,
            subscription.MaxUsers,
            subscription.IsFullAccess,
            subscription.FullAccessGrantedByAdminId,
            subscription.FullAccessGrantedAt,
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd,
            subscription.TrialEndsAt,
            subscription.CanceledAt);
    }
}
