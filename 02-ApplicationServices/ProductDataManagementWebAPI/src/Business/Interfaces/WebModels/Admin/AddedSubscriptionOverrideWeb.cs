namespace Business.Interfaces.WebModels.Admin;

public sealed record AddedSubscriptionOverrideWeb(
    Guid Id,
    string Key,
    string Value,
    string Reason,
    DateTime? ExpiresAt,
    DateTime CreatedAt);
