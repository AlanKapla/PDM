namespace Business.Interfaces.WebModels.Admin;

public sealed record ColdMailHistoryWeb(
    Guid Id,
    Guid BatchId,
    string RecipientEmail,
    string Subject,
    string Body,
    string HtmlBody,
    string Status,
    string? ErrorMessage,
    Guid SentByUserId,
    DateTime SentAt);
