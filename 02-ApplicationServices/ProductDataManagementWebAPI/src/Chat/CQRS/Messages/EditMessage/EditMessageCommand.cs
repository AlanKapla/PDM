using CQRS;
using MediatR;

namespace Chat.CQRS.Messages.EditMessage;

/// <summary>
/// Edits the content of a message. Only the author may edit within the configured time window.
/// </summary>
public sealed record EditMessageCommand(
    Guid ChatId,
    Guid MessageId,
    string NewContent) : IRequestCommand<Unit>;
