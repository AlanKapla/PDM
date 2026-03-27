using CQRS;
using MediatR;

namespace Chat.CQRS.Conversations.LeaveChat;

/// <summary>
/// The current user leaves a chat.
/// Non-admin: removed from the chat; group shrinks accordingly.
/// Admin: the entire group is dissolved and all members are notified.
/// </summary>
public sealed record LeaveChatCommand(Guid ChatId) : IRequestCommand<Unit>;
