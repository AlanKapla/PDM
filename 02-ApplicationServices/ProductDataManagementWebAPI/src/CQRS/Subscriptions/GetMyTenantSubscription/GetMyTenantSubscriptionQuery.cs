using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Subscriptions;
using CQRS.Behaviours;

namespace CQRS.Subscriptions.GetMyTenantSubscription;

public sealed record GetMyTenantSubscriptionQuery(Guid TenantId)
    : IRequestQuery<TenantSubscriptionInfoWeb>, IAuthorizableRequest, IBypassSubscriptionCheck
{
    public string PermissionCode => PermissionCodes.TenantView;
    public ResourceRef GetResource() => new(TenantId: TenantId);
}
