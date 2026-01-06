using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Files.ShareProjectFiles
{
    /// <summary>
    /// Command do udostępnienia plików wielu członkom projektu
    /// </summary>
    public record ShareProjectFilesCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public List<Guid> ProjectFileIds { get; init; } = new();
        
        /// <summary>
        /// Lista ID użytkowników (członków projektu), którym zostaną udostępnione pliki
        /// </summary>
        public List<Guid> SharedWithUserIds { get; init; } = new();

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
