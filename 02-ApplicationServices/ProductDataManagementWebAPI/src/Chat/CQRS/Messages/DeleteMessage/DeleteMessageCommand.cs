using CQRS;
using MediatR;

namespace Chat.CQRS.Messages.DeleteMessage;

/// <summary>
/// Soft-deletes a message. The author or a chat admin may delete it.
/// Deleted messages remain visible in history with content hidden.
/// </summary>
public sealed record DeleteMessageCommand(Guid ChatId, Guid MessageId) : IRequestCommand<Unit>;
