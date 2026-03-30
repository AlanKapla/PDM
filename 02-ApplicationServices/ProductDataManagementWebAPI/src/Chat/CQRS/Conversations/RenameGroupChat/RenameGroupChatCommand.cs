using CQRS;
using MediatR;

namespace Chat.CQRS.Conversations.RenameGroupChat;

/// <summary>
/// Renames a group chat. Only an admin member of the chat may perform this action.
/// </summary>
public sealed record RenameGroupChatCommand(Guid ChatId, string NewName) : IRequestCommand<Unit>;
