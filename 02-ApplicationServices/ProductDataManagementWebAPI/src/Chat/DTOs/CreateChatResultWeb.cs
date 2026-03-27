namespace Chat.DTOs;

/// <summary>
/// Response returned after creating a chat.
/// </summary>
public record CreateChatResultWeb(Guid Id, bool IsGroupChat);
