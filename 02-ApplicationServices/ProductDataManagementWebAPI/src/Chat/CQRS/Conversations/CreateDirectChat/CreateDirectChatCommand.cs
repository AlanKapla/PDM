using Business.Interfaces.WebModels.Chats;
using CQRS;

namespace Chat.CQRS.Conversations.CreateDirectChat;

/// <summary>
/// Creates a 1-1 direct chat between the current user and the target user.
/// Idempotent: returns the existing chat if one already exists between the two users.
/// Membership-based authorization: callers must share at least one project with the target.
/// </summary>
public sealed record CreateDirectChatCommand : IRequestCommand<CreateChatResultWeb>
{
    public required Guid TargetUserId { get; init; }
}
