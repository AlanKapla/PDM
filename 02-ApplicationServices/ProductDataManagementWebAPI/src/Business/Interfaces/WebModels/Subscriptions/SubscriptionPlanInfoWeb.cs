namespace Business.Interfaces.WebModels.Subscriptions;

public sealed record SubscriptionPlanInfoWeb(
    int Plan,
    string Name,
    int MaxProjects,
    int MaxUsers,
    decimal Price,
    string Currency);
