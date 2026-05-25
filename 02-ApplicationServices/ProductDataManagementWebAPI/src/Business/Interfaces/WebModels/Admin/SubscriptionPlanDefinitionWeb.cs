using Entities.Enums;

namespace Business.Interfaces.WebModels.Admin;

public record SubscriptionPlanDefinitionWeb(
    Guid Id,
    string Plan,
    string Name,
    int MaxProjects,
    int MaxUsers,
    decimal Price,
    string Currency,
    bool IsActive);
