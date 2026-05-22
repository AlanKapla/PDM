using CQRS;
using MediatR;

namespace Chat.CQRS.Messages.MarkAsRead;

/// <summary>
/// Marks all messages in a chat as read for the current user.
/// Broadcasts a read receipt to all other active members.
/// </summary>
/// <remarks>
/// <see cref="TenantId"/> is optional — see
/// <see cref="EditMessage.EditMessageCommand"/> for the shared-routing rationale.
/// </remarks>
public sealed record MarkAsReadCommand : IRequestCommand<Unit>
{
    public Guid? TenantId { get; init; }
    public required Guid ChatId { get; init; }
}
