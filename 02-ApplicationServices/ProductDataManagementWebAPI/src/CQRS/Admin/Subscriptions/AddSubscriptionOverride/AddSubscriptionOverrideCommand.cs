using Business.Interfaces.WebModels.Admin;

namespace CQRS.Admin.Subscriptions.AddSubscriptionOverride;

public sealed record AddSubscriptionOverrideCommand : IRequestCommand<AddedSubscriptionOverrideWeb>, ISuperAdminRequest
{
    public Guid TenantId { get; init; }
    public required string Key { get; init; }
    public required string Value { get; init; }
    public required string Reason { get; init; }
    public DateTime? ExpiresAt { get; init; }
}
