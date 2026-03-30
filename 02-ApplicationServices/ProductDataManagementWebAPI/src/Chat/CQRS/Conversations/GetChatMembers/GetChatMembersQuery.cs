using Chat.DTOs;
using CQRS;

namespace Chat.CQRS.Conversations.GetChatMembers;

/// <summary>
/// Returns all members of a chat. The requesting user must be a member.
/// </summary>
public sealed record GetChatMembersQuery(Guid ChatId) : IRequestQuery<List<ChatMemberWeb>>;
