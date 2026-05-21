namespace Business.Interfaces.WebModels.Chats;

/// <summary>
/// Response returned after creating a chat.
/// </summary>
public sealed record CreateChatResultWeb(Guid Id, bool IsGroupChat);
