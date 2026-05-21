using CQRS;
using MediatR;

namespace Chat.CQRS.Messages.EditMessage;

/// <summary>
/// Edits the content of a message. Only the author may edit within the configured time window.
/// </summary>
/// <remarks>
/// <see cref="TenantId"/> is optional — the tenant controller passes it from
/// the route, the direct-chats controller leaves it null. Authorization is
/// enforced at the controller via <c>[Authorize(Policy)]</c>.
/// </remarks>
public sealed record EditMessageCommand : IRequestCommand<Unit>
{
    public Guid? TenantId { get; init; }
    public required Guid ChatId { get; init; }
    public required Guid MessageId { get; init; }
    public required string NewContent { get; init; }
}
