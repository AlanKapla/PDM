using Business.Interfaces.Constants;
using Chat.CQRS.Shared;
using CQRS;
using MediatR;

namespace Chat.CQRS.Conversations.RenameGroupChat;

/// <summary>
/// Renames a group chat. Only an admin member of the chat may perform this action.
/// </summary>
public sealed record RenameGroupChatCommand : ChatScopedRequestBase, IRequestCommand<Unit>
{
    public required string NewName { get; init; }

    public override string PermissionCode => PermissionCodes.ChatRename;
}
