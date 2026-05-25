namespace Business.Interfaces.WebModels.Subscriptions;

public sealed record MockPaymentResultWeb(
    Guid PaymentId,
    decimal Amount,
    string Currency,
    string Status,
    DateTime PaidAt,
    DateTime PeriodEnd,
    DateTime NextPaymentDue);
