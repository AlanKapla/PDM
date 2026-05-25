using Business.Interfaces.WebModels.Admin;

namespace CQRS.Admin.Subscriptions.GetTenantSubscription;

public sealed record GetTenantSubscriptionQuery(Guid TenantId)
    : IRequestQuery<TenantSubscriptionWeb>, ISuperAdminRequest;
