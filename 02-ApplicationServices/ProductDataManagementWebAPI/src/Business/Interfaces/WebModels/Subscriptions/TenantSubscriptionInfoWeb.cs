namespace Business.Interfaces.WebModels.Subscriptions;

public sealed record TenantSubscriptionInfoWeb(
    Guid TenantId,
    int Plan,
    int Status,
    int MaxProjects,
    int MaxUsers,
    bool IsFullAccess,
    DateTime CurrentPeriodStart,
    DateTime? CurrentPeriodEnd,
    DateTime? TrialEndsAt,
    DateTime? CanceledAt);
