using Business.Interfaces.Constants;
using CQRS.Files._Shared;
using MediatR;

namespace CQRS.Files.SharePackages
{
    /// <summary>
    /// Command do udostępnienia paczek członkom projektu
    /// Udostępnia CAŁE paczki (bez wykluczeń plików)
    /// </summary>
    public sealed record SharePackagesCommand : ProjectScopedFilesRequestBase, IRequestCommand<Unit>
    {
        /// <summary>
        /// Lista ID paczek do udostępnienia
        /// </summary>
        public required List<Guid> PackageIds { get; init; }

        /// <summary>
        /// Lista ID użytkowników (członków projektu), którym zostaną udostępnione paczki
        /// </summary>
        public required List<Guid> SharedWithUserIds { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectResourcesShare;
    }
}

