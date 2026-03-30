using CQRS;
using MediatR;

namespace Chat.CQRS.Messages.MarkAsRead;

/// <summary>
/// Marks all messages in a chat as read for the current user.
/// Broadcasts a read receipt to all other active members.
/// </summary>
public sealed record MarkAsReadCommand(Guid ChatId) : IRequestCommand<Unit>;
