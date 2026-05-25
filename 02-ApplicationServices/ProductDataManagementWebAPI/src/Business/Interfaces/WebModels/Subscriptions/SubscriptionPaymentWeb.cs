namespace Business.Interfaces.WebModels.Subscriptions;

public sealed record SubscriptionPaymentWeb(
    Guid Id,
    int Plan,
    string PlanName,
    decimal Amount,
    string Currency,
    int Status,
    string StatusLabel,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    DateTime? PaidAt,
    string? FailureReason,
    DateTime CreatedAt);
