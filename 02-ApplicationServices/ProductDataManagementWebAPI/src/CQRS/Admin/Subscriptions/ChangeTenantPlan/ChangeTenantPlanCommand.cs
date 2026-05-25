using Business.Interfaces.WebModels.Admin;
using Entities.Enums;

namespace CQRS.Admin.Subscriptions.ChangeTenantPlan;

public sealed record ChangeTenantPlanCommand : IRequestCommand<TenantSubscriptionSummaryWeb>, ISuperAdminRequest
{
    public Guid TenantId { get; init; }
    public required SubscriptionPlan Plan { get; init; }
}
