using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Subscriptions;
using CQRS.Behaviours;
using Entities.Enums;

namespace CQRS.Subscriptions.RequestPlanChange;

public sealed record RequestPlanChangeCommand : IRequestCommand<TenantSubscriptionInfoWeb>, IAuthorizableRequest, IBypassSubscriptionCheck
{
    public Guid TenantId { get; init; }
    public required SubscriptionPlan Plan { get; init; }

    public string PermissionCode => PermissionCodes.TenantEdit;
    public ResourceRef GetResource() => new(TenantId: TenantId);
}
