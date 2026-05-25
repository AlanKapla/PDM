using MediatR;

namespace CQRS.Admin.Subscriptions.DeactivateSubscriptionOverride;

public sealed record DeactivateSubscriptionOverrideCommand(Guid TenantId, Guid OverrideId)
    : IRequestCommand<Unit>, ISuperAdminRequest;
