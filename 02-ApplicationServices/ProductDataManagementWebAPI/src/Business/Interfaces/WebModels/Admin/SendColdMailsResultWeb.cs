namespace Business.Interfaces.WebModels.Admin;

public sealed record SendColdMailsResultWeb(
    Guid BatchId,
    int QueuedCount,
    int FailedCount,
    IReadOnlyList<ColdMailSendItemWeb> Items);

public sealed record ColdMailSendItemWeb(
    string RecipientEmail,
    string Status,
    string? ErrorMessage);
