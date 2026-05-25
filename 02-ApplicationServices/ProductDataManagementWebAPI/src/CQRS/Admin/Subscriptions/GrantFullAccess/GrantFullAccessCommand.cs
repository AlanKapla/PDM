using Business.Interfaces.WebModels.Admin;

namespace CQRS.Admin.Subscriptions.GrantFullAccess;

public sealed record GrantFullAccessCommand(Guid TenantId)
    : IRequestCommand<GrantFullAccessResultWeb>, ISuperAdminRequest;
