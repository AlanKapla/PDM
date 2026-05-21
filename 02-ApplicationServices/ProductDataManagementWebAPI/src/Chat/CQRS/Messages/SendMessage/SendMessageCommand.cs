using CQRS;

namespace Chat.CQRS.Messages.SendMessage;

/// <summary>
/// Sends a message to a chat. The sender must be a member of the chat.
/// After persisting, the message is broadcast to all chat members via SignalR.
/// </summary>
/// <remarks>
/// <see cref="TenantId"/> is optional — see
/// <see cref="GetChatMessages.GetChatMessagesQuery"/> for the shared-routing rationale.
/// Authorization for the tenant scope is enforced by the tenant controller's
/// <c>[Authorize(Policy)]</c> attribute.
/// </remarks>
public sealed record SendMessageCommand : IRequestCommand<Guid>
{
    public Guid? TenantId { get; init; }
    public required Guid ChatId { get; init; }
    public required string Content { get; init; }
    public Guid? ReplyToMessageId { get; init; }
}
