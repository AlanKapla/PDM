namespace Business.Interfaces.WebModels.Chats;

/// <summary>
/// API-facing DTO for a chat conversation.
/// </summary>
public sealed record ChatWeb(
    Guid Id,
    string Name,
    bool IsGroupChat,
    Guid? ProjectId,
    Guid? TenantId,
    DateTime CreatedAt,
    Guid CreatedByUserId,
    int UnreadCount,
    MessageWeb? LastMessage,
    List<ChatMemberWeb> Members);
