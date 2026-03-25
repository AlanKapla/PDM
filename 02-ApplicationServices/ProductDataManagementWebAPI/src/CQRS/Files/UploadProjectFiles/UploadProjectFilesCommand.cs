using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Files.UploadProjectFiles
{
    /// <summary>
    /// Command do przesyłania plików do projektu
    /// </summary>
    public record UploadProjectFilesCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public Guid ProjectFilePackageId { get; init; }
        
        /// <summary>
        /// Lista plików do przesłania z opcjonalnymi nazwami wyświetlanymi
        /// </summary>
        public List<FileUploadItem> Files { get; init; } = new();

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }

    }
