namespace Chat.DTOs;

/// <summary>
/// API-facing DTO for a single chat message.
/// Content is empty when IsDeleted is true — clients should render a placeholder.
/// </summary>
public record MessageWeb(
    Guid Id,
    Guid ChatId,
    Guid SenderId,
    string SenderFirstName,
    string SenderLastName,
    string Content,
    bool IsDeleted,
    bool IsEdited,
    DateTime SentAt,
    DateTime? EditedAt,
    Guid? ReplyToMessageId);
