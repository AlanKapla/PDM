using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.Admin;
using Entities.Models.Subscriptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Admin.Subscriptions.GetTenantSubscription;

public sealed class GetTenantSubscriptionQueryHandler
    : IRequestHandler<GetTenantSubscriptionQuery, TenantSubscriptionWeb>
{
    private readonly IReadRepository<TenantSubscription> subscriptionRepo;

    public GetTenantSubscriptionQueryHandler(IReadRepository<TenantSubscription> subscriptionRepo)
    {
        this.subscriptionRepo = subscriptionRepo;
    }

    public async Task<TenantSubscriptionWeb> Handle(
        GetTenantSubscriptionQuery request,
        CancellationToken cancellationToken)
    {
        TenantSubscription? subscription = await subscriptionRepo.GetFirstBySearch(
            s => s.TenantId == request.TenantId,
            cancellationToken,
            q => q.Include(s => s.Overrides));

        if (subscription is null)
        {
            throw new NotFoundApiException(nameof(TenantSubscription), request.TenantId.ToString());
        }

        return MapToWeb(subscription);
    }

    private static TenantSubscriptionWeb MapToWeb(TenantSubscription subscription)
    {
        IEnumerable<SubscriptionOverrideWeb> overrides = subscription.Overrides
            .Select(o => new SubscriptionOverrideWeb(
                o.Id,
                o.Key,
                o.Value,
                o.Reason,
                o.SetByAdminId,
                o.ExpiresAt,
                o.IsActive,
                o.IsValid()));

        return new TenantSubscriptionWeb(
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
            subscription.CanceledAt,
            overrides);
    }
}
