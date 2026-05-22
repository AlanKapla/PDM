using Business.Interfaces.WebModels.Chats;
using CQRS;

namespace Chat.CQRS.Conversations.SearchChats;

/// <summary>
/// Searches chats the current user belongs to.
/// Matches against: chat name, member full names, and message content.
/// Returns matching chats; MatchingMessageIds is populated for message-content matches.
/// When <paramref name="TenantId"/> is provided, results are restricted to that tenant.
/// </summary>
public sealed record SearchChatsQuery(string Phrase, Guid? TenantId = null)
    : IRequestQuery<List<ChatSearchResultWeb>>;
