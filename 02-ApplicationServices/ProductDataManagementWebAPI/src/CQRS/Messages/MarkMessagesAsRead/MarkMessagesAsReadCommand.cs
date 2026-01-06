using Business.Interfaces.Constants;
using Business.Interfaces.Model;

namespace CQRS.Messages.MarkMessagesAsRead
{
    public sealed record MarkMessagesAsReadCommand(
        Guid TenantId,
        Guid ProjectId,
        Guid ChatId
    ) : IRequestCommand<int>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
