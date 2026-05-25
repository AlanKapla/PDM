using Business.Interfaces.WebModels.Subscriptions;

namespace CQRS.Admin.Subscriptions.GetAdminPaymentHistory;

public sealed record GetAdminPaymentHistoryQuery(Guid TenantId)
    : IRequestQuery<IEnumerable<SubscriptionPaymentWeb>>, ISuperAdminRequest;
