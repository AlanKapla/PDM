using CQRS;
using MediatR;

namespace Chat.CQRS.Conversations.RemoveChatMember;

/// <summary>
/// Removes a member from a group chat.
/// A member can remove themselves (leave). An admin can remove any non-admin member.
/// </summary>
public sealed record RemoveChatMemberCommand(Guid ChatId, Guid UserId) : IRequestCommand<Unit>;
