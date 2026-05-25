using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.Subscriptions;
using Entities.Models.Subscriptions;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Subscriptions.GetSubscriptionStatus;

public sealed class GetSubscriptionStatusQueryHandler
    : IRequestHandler<GetSubscriptionStatusQuery, SubscriptionStatusWeb>
{
    private readonly IReadRepository<TenantSubscription> subscriptionRepo;
    private readonly IReadRepository<SubscriptionPlanDefinition> planRepo;

    public GetSubscriptionStatusQueryHandler(
        IReadRepository<TenantSubscription> subscriptionRepo,
        IReadRepository<SubscriptionPlanDefinition> planRepo)
    {
        this.subscriptionRepo = subscriptionRepo;
        this.planRepo         = planRepo;
    }

    public async Task<SubscriptionStatusWeb> Handle(
        GetSubscriptionStatusQuery request,
        CancellationToken cancellationToken)
    {
        TenantSubscription subscription = await GetAndValidateSubscriptionAsync(
            request.TenantId, cancellationToken);

        SubscriptionPlanDefinition planDefinition = await GetPlanDefinitionAsync(
            subscription, cancellationToken);

        return MapToWeb(subscription, planDefinition);
    }

    private async Task<TenantSubscription> GetAndValidateSubscriptionAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        TenantSubscription? subscription = await subscriptionRepo.GetFirstBySearch(
            s => s.TenantId == tenantId,
            cancellationToken);

        if (subscription is null)
        {
            throw new NotFoundApiException(nameof(TenantSubscription), tenantId.ToString());
        }

        return subscription;
    }

    private async Task<SubscriptionPlanDefinition> GetPlanDefinitionAsync(
        TenantSubscription subscription,
        CancellationToken cancellationToken)
    {
        SubscriptionPlanDefinition? planDefinition = await planRepo.GetFirstBySearch(
            p => p.Plan == subscription.Plan && p.IsActive,
            cancellationToken);

        if (planDefinition is null)
        {
            throw new NotFoundApiException(nameof(SubscriptionPlanDefinition), subscription.Plan.ToString());
        }

        return planDefinition;
    }

    private static SubscriptionStatusWeb MapToWeb(
        TenantSubscription subscription,
        SubscriptionPlanDefinition planDefinition) =>
        new(
            Plan:             (int)subscription.Plan,
            PlanName:         planDefinition.Name,
            Status:           (int)subscription.Status,
            StatusLabel:      subscription.Status.ToString(),
            NextPaymentDue:   subscription.NextPaymentDue,
            LastPaidAt:       subscription.LastPaidAt,
            LastPaidAmount:   subscription.LastPaidAmount,
            Currency:         planDefinition.Currency,
            GracePeriodEndsAt: subscription.GracePeriodEndsAt,
            CurrentPeriodEnd: subscription.CurrentPeriodEnd,
            Price:            planDefinition.Price,
            IsCurrentPeriodPaid: subscription.LastPaidAt.HasValue
                && subscription.LastPaidAt.Value >= subscription.CurrentPeriodStart);
}
