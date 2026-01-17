using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Messages;

namespace CQRS.Chats.GetProjectChats
{
    public sealed record GetProjectChatsQuery(
        Guid TenantId,
        Guid ProjectId
    ) : IRequestQuery<List<ChatWeb>>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectResourcesRead;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
