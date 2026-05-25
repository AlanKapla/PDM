namespace Business.Interfaces.WebModels.Admin;

public sealed record AdminSubscriptionPlanWeb(
    Guid Id,
    int Plan,
    string Name,
    int MaxProjects,
    int MaxUsers,
    decimal Price,
    string Currency,
    bool IsActive,
    DateTime? UpdatedAt);
