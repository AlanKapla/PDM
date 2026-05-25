using Business.Interfaces.WebModels.Admin;
using Entities.Enums;

namespace CQRS.Admin.Subscriptions.UpdatePlanDefinition;

public sealed record UpdatePlanDefinitionCommand : IRequestCommand<AdminSubscriptionPlanWeb>, ISuperAdminRequest
{
    public required SubscriptionPlan Plan { get; init; }
    public required string Name { get; init; }
    public required int MaxProjects { get; init; }
    public required int MaxUsers { get; init; }
    public required decimal Price { get; init; }
    public required string Currency { get; init; }
    public required bool IsActive { get; init; }
}
