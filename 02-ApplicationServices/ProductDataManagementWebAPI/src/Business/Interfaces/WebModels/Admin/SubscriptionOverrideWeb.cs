namespace Business.Interfaces.WebModels.Admin;

public sealed record SubscriptionOverrideWeb(
    Guid Id,
    string Key,
    string Value,
    string Reason,
    Guid SetByAdminId,
    DateTime? ExpiresAt,
    bool IsActive,
    bool IsValid);
