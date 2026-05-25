using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Enums;
using Entities.Models.Subscriptions;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Behaviours;

public sealed class SubscriptionEnforcementBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICurrentUser currentUser;
    private readonly IReadRepository<TenantSubscription> subscriptionRepository;
    private readonly ILogger<SubscriptionEnforcementBehavior<TRequest, TResponse>> logger;

    public SubscriptionEnforcementBehavior(
        ICurrentUser currentUser,
        IReadRepository<TenantSubscription> subscriptionRepository,
        ILogger<SubscriptionEnforcementBehavior<TRequest, TResponse>> logger)
    {
        this.currentUser = currentUser;
        this.subscriptionRepository = subscriptionRepository;
        this.logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is IBypassSubscriptionCheck)
        {
            return await next(cancellationToken);
        }

        if (request is not IAuthorizableRequest authorizableRequest)
        {
            return await next(cancellationToken);
        }

        Guid tenantId = authorizableRequest.GetResource().TenantId;

        if (tenantId == Guid.Empty)
        {
            return await next(cancellationToken);
        }

        TenantSubscription? subscription = await subscriptionRepository.GetFirstBySearch(
            s => s.TenantId == tenantId,
            cancellationToken);

        if (subscription is null)
        {
            return await next(cancellationToken);
        }

        if (!IsSubscriptionBlocked(subscription.Status))
        {
            return await next(cancellationToken);
        }

        //if (currentUser.IsSuperAdmin)
        //{
        //    return await next(cancellationToken);
        //}

        // TenantAdmin NIE jest przepuszczany — dostęp do zasobów jest zablokowany dla wszystkich,
        // włącznie z adminem. Admin może tylko opłacić subskrypcję (IBypassSubscriptionCheck).
        // Wyjątek: ChangeActiveTenantCommandHandler ma własną logikę i zwraca IsSubscriptionBlocked=true.

        logger.LogWarning(
            "Access blocked for user {UserId} on request {RequestType}. Tenant {TenantId} subscription status: {Status}.",
            currentUser.Id,
            typeof(TRequest).Name,
            tenantId,
            subscription.Status);

        throw new SubscriptionSuspendedException(tenantId);
    }

    private static bool IsSubscriptionBlocked(SubscriptionStatus status)
        => status is SubscriptionStatus.PastDue or SubscriptionStatus.Canceled or SubscriptionStatus.GracePeriod;
}
