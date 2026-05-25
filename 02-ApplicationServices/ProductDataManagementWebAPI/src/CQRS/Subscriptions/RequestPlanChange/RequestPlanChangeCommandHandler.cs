using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Subscriptions;
using Entities.Enums;
using Entities.Models.Subscriptions;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Subscriptions.RequestPlanChange;

public sealed class RequestPlanChangeCommandHandler
    : IRequestHandler<RequestPlanChangeCommand, TenantSubscriptionInfoWeb>
{
    private readonly IReadRepository<TenantSubscription> subscriptionReadRepo;
    private readonly IRepository<TenantSubscription> subscriptionRepo;
    private readonly IReadRepository<SubscriptionPlanDefinition> planRepo;
    private readonly ISubscriptionBillingService billingService;

    public RequestPlanChangeCommandHandler(
        IReadRepository<TenantSubscription> subscriptionReadRepo,
        IRepository<TenantSubscription> subscriptionRepo,
        IReadRepository<SubscriptionPlanDefinition> planRepo,
        ISubscriptionBillingService billingService)
    {
        this.subscriptionReadRepo = subscriptionReadRepo;
        this.subscriptionRepo     = subscriptionRepo;
        this.planRepo             = planRepo;
        this.billingService       = billingService;
    }

    public async Task<TenantSubscriptionInfoWeb> Handle(
        RequestPlanChangeCommand request,
        CancellationToken cancellationToken)
    {
        TenantSubscription subscription = await GetAndValidateSubscriptionAsync(
            request.TenantId, cancellationToken);

        SubscriptionPlanDefinition planDefinition = await GetAndValidatePlanAsync(
            request.Plan, cancellationToken);

        bool switchingToPaid = subscription.Plan == SubscriptionPlan.Free
                               && request.Plan != SubscriptionPlan.Free;

        if (switchingToPaid)
        {
            // InitializeBillingAsync wewnętrznie woła ApplyPlan + ustawia daty
            await billingService.InitializeBillingAsync(request.TenantId, request.Plan, cancellationToken);
        }
        else if (request.Plan != SubscriptionPlan.Free)
        {
            // Zmiana między planami płatnymi — zainicjalizuj nowy okres; płatność triggeruje UI
            await billingService.InitializeBillingAsync(request.TenantId, request.Plan, cancellationToken);
        }
        else
        {
            // Downgrade do Free — usuń billing dates
            subscription.ApplyPlan(planDefinition);
            subscription.NextPaymentDue    = null;
            subscription.GracePeriodEndsAt = null;
            subscription.Status            = SubscriptionStatus.Active;
            await subscriptionRepo.Update(subscription);
            await subscriptionRepo.SaveChangesAsync(cancellationToken);
        }

        TenantSubscription updated = await GetAndValidateSubscriptionAsync(request.TenantId, cancellationToken);
        return MapToWeb(updated);
    }

    private async Task<TenantSubscription> GetAndValidateSubscriptionAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        TenantSubscription? subscription = await subscriptionReadRepo.GetFirstBySearch(
            s => s.TenantId == tenantId,
            cancellationToken);

        if (subscription is null)
        {
            throw new NotFoundApiException(nameof(TenantSubscription), tenantId.ToString());
        }

        return subscription;
    }

    private async Task<SubscriptionPlanDefinition> GetAndValidatePlanAsync(
        Entities.Enums.SubscriptionPlan plan,
        CancellationToken cancellationToken)
    {
        SubscriptionPlanDefinition? planDefinition = await planRepo.GetFirstBySearch(
            p => p.Plan == plan && p.IsActive,
            cancellationToken);

        if (planDefinition is null)
        {
            throw new NotFoundApiException(nameof(SubscriptionPlanDefinition), plan.ToString());
        }

        return planDefinition;
    }

    private static TenantSubscriptionInfoWeb MapToWeb(TenantSubscription subscription) =>
        new(
            subscription.TenantId,
            (int)subscription.Plan,
            (int)subscription.Status,
            subscription.MaxProjects,
            subscription.MaxUsers,
            subscription.IsFullAccess,
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd,
            subscription.TrialEndsAt,
            subscription.CanceledAt);
}
