using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;
using CQRS.Files;

namespace CQRS.Files.CreatePackageAndUploadFiles
{
    /// <summary>
    /// Command to create a new package and upload files to it
    /// </summary>
    public record CreatePackageAndUploadFilesCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public string PackageName { get; init; } = default!;
        
        /// <summary>
        /// Lista plików do przesłania z opcjonalnymi nazwami wyświetlanymi
        /// </summary>
        public List<FileUploadItem> Files { get; init; } = new();

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }

    }
