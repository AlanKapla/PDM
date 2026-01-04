using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using CQRS.Interfaces;
using MediatR;

namespace CQRS.Files.UpdateFileShare
{
    /// <summary>
    /// Command to update file sharing - add or remove access for specific users
    /// </summary>
    public record UpdateFileShareCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
        public Guid FileId { get; init; }
        
        /// <summary>
        /// Lista ID użytkowników, którzy powinni mieć dostęp do pliku
        /// Użytkownicy nie na liście zostaną usunięci z udostępnienia
        /// </summary>
        public List<Guid> SharedWithUserIds { get; init; } = new();

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;
        
        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
