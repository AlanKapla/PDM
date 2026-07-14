using Business.Interfaces.WebModels.Chats;
using CQRS;

namespace Chat.CQRS.Conversations.CreateGroupChat;

/// <summary>
/// Creates a group chat (3+ members) bound to a tenant and optionally a project.
/// The current user is implicitly added as admin; MemberUserIds lists the additional members.
/// </summary>
public sealed record CreateGroupChatCommand : IRequestCommand<CreateChatResultWeb>
{
    public required Guid TenantId { get; init; }
    public Guid? ProjectId { get; init; }
    public required List<Guid> MemberUserIds { get; init; }
    public string? Name { get; init; }
}
