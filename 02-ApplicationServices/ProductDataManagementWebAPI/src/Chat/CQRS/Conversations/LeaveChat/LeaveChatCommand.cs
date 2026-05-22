using CQRS;
using MediatR;

namespace Chat.CQRS.Conversations.LeaveChat;

/// <summary>
/// The current user leaves a chat.
/// Non-admin: removed from the chat; group shrinks accordingly.
/// Admin: the entire group is dissolved and all members are notified.
/// Membership-based: no role permission required (the caller acts on their own membership).
/// </summary>
/// <remarks>
/// <see cref="TenantId"/> is optional — group-leave is invoked from the tenant
/// controller (passes TenantId from route); direct-chat leave is invoked from
/// the direct controller (TenantId left null).
/// </remarks>
public sealed record LeaveChatCommand : IRequestCommand<Unit>
{
    public Guid? TenantId { get; init; }
    public required Guid ChatId { get; init; }
}
