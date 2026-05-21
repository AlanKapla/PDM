using Business.Interfaces.Constants;
using Chat.CQRS.Shared;
using CQRS;
using MediatR;

namespace Chat.CQRS.Conversations.RemoveChatMember;

/// <summary>
/// Removes a member from a group chat.
/// A member can remove themselves (leave). An admin can remove any non-admin member.
/// </summary>
public sealed record RemoveChatMemberCommand : ChatScopedRequestBase, IRequestCommand<Unit>
{
    public required Guid UserId { get; init; }

    public override string PermissionCode => PermissionCodes.ChatMembersManage;
}
