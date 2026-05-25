using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Subscriptions;
using CQRS.Behaviours;

namespace CQRS.Subscriptions.GetPaymentHistory;

public sealed record GetPaymentHistoryQuery(Guid TenantId)
    : IRequestQuery<IEnumerable<SubscriptionPaymentWeb>>, IAuthorizableRequest, IBypassSubscriptionCheck
{
    public string PermissionCode => PermissionCodes.TenantView;
    public ResourceRef GetResource() => new(TenantId: TenantId);
}
