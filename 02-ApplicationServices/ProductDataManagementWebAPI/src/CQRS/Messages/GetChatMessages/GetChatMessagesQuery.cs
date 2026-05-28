using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Messages;

namespace CQRS.Messages.GetChatMessages
{
    public sealed record GetChatMessagesQuery(
        Guid TenantId,
        Guid ProjectId,
        Guid ChatId,
        int PageNumber = 1,
        int PageSize = 50
    ) : IRequestQuery<List<MessageWeb>>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectSettings;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
