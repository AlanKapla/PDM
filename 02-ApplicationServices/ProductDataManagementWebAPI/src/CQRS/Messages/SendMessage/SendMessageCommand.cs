using Business.Interfaces.Constants;
using Business.Interfaces.Model;

namespace CQRS.Messages.SendMessage
{
    public record SendMessageCommand(
        Guid TenantId,
        Guid ProjectId,
        Guid ChatId,
        string Content
    ) : IRequestCommand<Guid>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
