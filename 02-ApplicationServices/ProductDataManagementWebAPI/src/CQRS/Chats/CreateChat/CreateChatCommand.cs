using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using CQRS.Interfaces;
using CQRS;

namespace CQRS.Chats.CreateChat
{
    public sealed record CreateChatCommand(
        Guid TenantId,
        Guid ProjectId,
        string Name,
        bool IsGroupChat,
        List<Guid> MemberUserIds
    ) : IRequestCommand<Guid>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
