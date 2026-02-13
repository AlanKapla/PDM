using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Files.SharePackages
{
    /// <summary>
    /// Command do udostępnienia paczek członkom projektu
    /// Udostępnia CAŁE paczki (bez wykluczeń plików)
    /// </summary>
    public record SharePackagesCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        
        /// <summary>
        /// Lista ID paczek do udostępnienia
        /// </summary>
        public List<Guid> PackageIds { get; init; } = new();
        
        /// <summary>
        /// Lista ID użytkowników (członków projektu), którym zostaną udostępnione paczki
        /// </summary>
        public List<Guid> SharedWithUserIds { get; init; } = new();

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}


