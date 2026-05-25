using MediatR;

namespace CQRS.Admin.Subscriptions.RevokeFullAccess;

public sealed record RevokeFullAccessCommand(Guid TenantId)
    : IRequestCommand<Unit>, ISuperAdminRequest;
