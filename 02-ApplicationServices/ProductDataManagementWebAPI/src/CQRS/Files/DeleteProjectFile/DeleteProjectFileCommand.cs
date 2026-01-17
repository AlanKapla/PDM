using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Files.DeleteProjectFile
{
    /// <summary>
    /// Command to delete a project file
    /// </summary>
    public sealed record DeleteProjectFileCommand(
        Guid TenantId,
        Guid ProjectId,
        Guid FileId
    ) : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
