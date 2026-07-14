using Chat.CQRS.Shared;
using CQRS;
using MediatR;

namespace Chat.CQRS.Conversations.AddChatMember;

/// <summary>
/// Adds a user to an existing chat. Works for both direct and group chats.
/// For group chats: caller must be admin; new member must be in chat.ProjectId.
/// For direct chats (converting to group): ProjectId is required; new member must be in that project.
/// IsGroupChat is recalculated after the addition (memberCount > 2).
/// </summary>
public sealed record AddChatMemberCommand : ChatScopedRequestBase, IRequestCommand<Unit>
{
    public required Guid UserId { get; init; }
}
