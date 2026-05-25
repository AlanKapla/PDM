using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Subscriptions;
using CQRS.Behaviours;

namespace CQRS.Subscriptions.ProcessMockPayment;

public sealed record ProcessMockPaymentCommand(Guid TenantId)
    : IRequestCommand<MockPaymentResultWeb>, IAuthorizableRequest, IBypassSubscriptionCheck
{
    public string PermissionCode => PermissionCodes.TenantEdit;
    public ResourceRef GetResource() => new(TenantId: TenantId);
}
