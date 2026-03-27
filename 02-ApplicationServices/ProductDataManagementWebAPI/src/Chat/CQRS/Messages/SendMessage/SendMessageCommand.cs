using CQRS;

namespace Chat.CQRS.Messages.SendMessage;

/// <summary>
/// Sends a message to a chat. The sender must be a member of the chat.
/// After persisting, the message is broadcast to all chat members via SignalR.
/// </summary>
public sealed record SendMessageCommand(
    Guid ChatId,
    string Content,
    Guid? ReplyToMessageId = null) : IRequestCommand<Guid>;
