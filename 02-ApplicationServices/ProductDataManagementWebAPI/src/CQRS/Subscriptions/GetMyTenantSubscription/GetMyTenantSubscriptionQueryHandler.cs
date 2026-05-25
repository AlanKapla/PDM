using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.Subscriptions;
using Entities.Models.Subscriptions;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Subscriptions.GetMyTenantSubscription;

public sealed class GetMyTenantSubscriptionQueryHandler
    : IRequestHandler<GetMyTenantSubscriptionQuery, TenantSubscriptionInfoWeb>
{
    private readonly IReadRepository<TenantSubscription> subscriptionRepo;

    public GetMyTenantSubscriptionQueryHandler(IReadRepository<TenantSubscription> subscriptionRepo)
    {
        this.subscriptionRepo = subscriptionRepo;
    }

    public async Task<TenantSubscriptionInfoWeb> Handle(
        GetMyTenantSubscriptionQuery request,
        CancellationToken cancellationToken)
    {
        TenantSubscription subscription = await GetAndValidateSubscriptionAsync(
            request.TenantId, cancellationToken);

        return MapToWeb(subscription);
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
