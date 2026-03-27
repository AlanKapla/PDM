using CQRS;
using MediatR;

namespace Chat.CQRS.Conversations.DeleteChat;

/// <summary>
/// Deletes a chat and all of its messages and members.
/// Group chat: admin only.
/// Direct chat: any member.
/// </summary>
public sealed record DeleteChatCommand(Guid ChatId) : IRequestCommand<Unit>;
