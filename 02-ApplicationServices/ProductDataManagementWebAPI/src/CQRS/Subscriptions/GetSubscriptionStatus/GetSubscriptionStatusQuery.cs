using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Subscriptions;
using CQRS.Behaviours;

namespace CQRS.Subscriptions.GetSubscriptionStatus;

public sealed record GetSubscriptionStatusQuery(Guid TenantId)
    : IRequestQuery<SubscriptionStatusWeb>, IAuthorizableRequest, IBypassSubscriptionCheck
{
    public string PermissionCode => PermissionCodes.TenantView;
    public ResourceRef GetResource() => new(TenantId: TenantId);
}
