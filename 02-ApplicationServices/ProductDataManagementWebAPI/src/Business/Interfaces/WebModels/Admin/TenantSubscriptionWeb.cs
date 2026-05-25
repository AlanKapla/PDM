namespace Business.Interfaces.WebModels.Admin;

public sealed record TenantSubscriptionWeb(
    Guid TenantId,
    int Plan,
    int Status,
    int MaxProjects,
    int MaxUsers,
    bool IsFullAccess,
    Guid? FullAccessGrantedByAdminId,
    DateTime? FullAccessGrantedAt,
    DateTime CurrentPeriodStart,
    DateTime? CurrentPeriodEnd,
    DateTime? TrialEndsAt,
    DateTime? CanceledAt,
    IEnumerable<SubscriptionOverrideWeb> Overrides);
