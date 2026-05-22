using Business.Interfaces.Constants;
using Chat.CQRS.Shared;
using CQRS;
using MediatR;

namespace Chat.CQRS.Conversations.DeleteChat;

/// <summary>
/// Deletes a chat and all of its messages and members.
/// Group chat: admin only.
/// Direct chat: any member.
/// </summary>
public sealed record DeleteChatCommand : ChatScopedRequestBase, IRequestCommand<Unit>
{
    public override string PermissionCode => PermissionCodes.ChatDelete;
}
