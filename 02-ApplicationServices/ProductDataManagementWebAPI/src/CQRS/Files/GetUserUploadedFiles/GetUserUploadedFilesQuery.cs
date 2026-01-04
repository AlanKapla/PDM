using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Files;
using CQRS.Interfaces;

namespace CQRS.Files.GetUserUploadedFiles
{
    /// <summary>
    /// Query do pobierania paczek z plikami przesłanymi przez użytkownika w projekcie
    /// </summary>
    public sealed record GetUserUploadedFilesQuery(
        Guid TenantId,
        Guid ProjectId
    ) : IRequestQuery<List<ProjectFilePackageWeb>>, IAuthorizableRequest
    {
        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
