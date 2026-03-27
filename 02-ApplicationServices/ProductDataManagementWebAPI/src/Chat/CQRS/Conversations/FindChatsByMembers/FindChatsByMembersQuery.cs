using Chat.DTOs;
using CQRS;

namespace Chat.CQRS.Conversations.FindChatsByMembers;

/// <summary>
/// Returns all chats that contain the current user and every user in MemberUserIds.
/// Useful for finding existing chats when composing a new conversation.
/// </summary>
public sealed record FindChatsByMembersQuery(List<Guid> MemberUserIds) : IRequestQuery<List<ChatWeb>>;
