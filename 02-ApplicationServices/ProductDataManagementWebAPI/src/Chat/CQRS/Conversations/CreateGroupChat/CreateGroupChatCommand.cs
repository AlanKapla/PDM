using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Chats;
using Chat.CQRS.Shared;
using CQRS;

namespace Chat.CQRS.Conversations.CreateGroupChat;

/// <summary>
/// Creates a group chat (3+ members) bound to a tenant and optionally a project.
/// The current user is implicitly added as admin; MemberUserIds lists the additional members.
/// Requires <see cref="PermissionCodes.ChatWrite"/>.
/// </summary>
public sealed record CreateGroupChatCommand : IRequestCommand<CreateChatResultWeb>, IAuthorizableRequest
{
    public required Guid TenantId { get; init; }
    public Guid? ProjectId { get; init; }
    public required List<Guid> MemberUserIds { get; init; }
    public string? Name { get; init; }

    public string PermissionCode => PermissionCodes.ChatWrite;

    public ResourceRef GetResource() =>
        new ResourceRef(TenantId: TenantId, ProjectId: ProjectId);
}
