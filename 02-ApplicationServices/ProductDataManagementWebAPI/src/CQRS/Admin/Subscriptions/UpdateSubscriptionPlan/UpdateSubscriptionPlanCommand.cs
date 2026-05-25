using Business.Interfaces.WebModels.Admin;

namespace CQRS.Admin.Subscriptions.UpdateSubscriptionPlan;

public sealed record UpdateSubscriptionPlanCommand(
    Guid Id,
    string Name,
    int MaxProjects,
    int MaxUsers,
    decimal Price,
    string Currency,
    bool IsActive)
    : IRequestCommand<SubscriptionPlanDefinitionWeb>, ISuperAdminRequest;
