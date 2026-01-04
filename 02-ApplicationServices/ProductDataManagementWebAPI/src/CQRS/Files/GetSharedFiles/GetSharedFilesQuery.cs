using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Files;
using CQRS.Interfaces;

namespace CQRS.Files.GetSharedFiles
{
    /// <summary>
    /// Query do pobierania plików udostępnionych użytkownikowi, zgrupowanych po paczkach
    /// </summary>
    public sealed record GetSharedFilesQuery(
        Guid TenantId,
        Guid ProjectId
    ) : IRequestQuery<List<SharedProjectFilePackageWeb>>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectResourcesReadShared;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
