using Business.Interfaces.Constants;
using CQRS.Files._Shared;
using MediatR;

namespace CQRS.Files.UpdateFileShare
{
    /// <summary>
    /// Command to update file sharing - add or remove access for specific users
    /// </summary>
    public sealed record UpdateFileShareCommand : FileScopedRequestBase, IRequestCommand<Unit>
    {
        /// <summary>
        /// Lista ID użytkowników, którzy powinni mieć dostęp do pliku
        /// Użytkownicy nie na liście zostaną usunięci z udostępnienia
        /// </summary>
        public required List<Guid> SharedWithUserIds { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectFiles;
    }
}
