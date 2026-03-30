using Chat.DTOs;
using CQRS;

namespace Chat.CQRS.Messages.GetChatMessages;

/// <summary>
/// Returns cursor-paginated messages for a chat, ordered newest → oldest.
/// Pass <c>Before</c> as the Id of the oldest message already held by the client
/// to load the next page. Omit it to receive the most recent page.
/// The requesting user must be a member of the chat.
/// </summary>
public sealed record GetChatMessagesQuery(
    Guid ChatId,
    Guid? Before = null,
    int PageSize = 50) : IRequestQuery<List<MessageWeb>>;
