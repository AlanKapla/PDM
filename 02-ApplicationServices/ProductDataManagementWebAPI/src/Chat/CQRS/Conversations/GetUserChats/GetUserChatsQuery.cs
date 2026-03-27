using Chat.DTOs;
using CQRS;

namespace Chat.CQRS.Conversations.GetUserChats;

/// <summary>
/// Returns all chats for the current user across all projects and tenants.
/// </summary>
public sealed record GetUserChatsQuery() : IRequestQuery<List<ChatWeb>>;
