using CQRS;
using MediatR;

namespace Chat.CQRS.Messages.DeleteMessage;

/// <summary>
/// Soft-deletes a message. The author or a chat admin may delete it.
/// Deleted messages remain visible in history with content hidden.
/// </summary>
/// <remarks>
/// <see cref="TenantId"/> is optional — see
/// <see cref="EditMessage.EditMessageCommand"/> for the shared-routing rationale.
/// </remarks>
public sealed record DeleteMessageCommand : IRequestCommand<Unit>
{
    public Guid? TenantId { get; init; }
    public required Guid ChatId { get; init; }
    public required Guid MessageId { get; init; }
}
