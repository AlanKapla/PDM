using Business.Interfaces.WebModels.Chats;
using Chat.CQRS.Shared;
using CQRS;

namespace Chat.CQRS.Conversations.GetAvailableMembers;

/// <summary>
/// Returns users who are members of the chat's project but not yet members of the chat.
/// Only applicable to group chats. The caller must be a member.
/// </summary>
public sealed record GetAvailableMembersQuery : ChatScopedRequestBase, IRequestQuery<List<AvailableMemberWeb>>
{
}
