namespace Business.Interfaces.WebModels.Subscriptions;

public sealed record SubscriptionStatusWeb(
    int Plan,
    string PlanName,
    int Status,
    string StatusLabel,
    DateTime? NextPaymentDue,
    DateTime? LastPaidAt,
    decimal? LastPaidAmount,
    string Currency,
    DateTime? GracePeriodEndsAt,
    DateTime? CurrentPeriodEnd,
    decimal Price,
    bool IsCurrentPeriodPaid);
