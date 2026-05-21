using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.Files;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    /// <summary>
    /// Default <see cref="IFileAccessGuard"/> implementation.
    /// Resource lookup uses read-only repositories; write operations remain in the calling handler.
    /// </summary>
    public sealed class FileAccessGuard : IFileAccessGuard
    {
        private readonly IReadRepository<ProjectFile> projectFileRepo;
        private readonly IReadRepository<ProjectFilePackage> packageRepo;
        private readonly IReadRepository<SharedProjectFile> sharedRepo;
        private readonly ICurrentUser currentUser;

        public FileAccessGuard(
            IReadRepository<ProjectFile> projectFileRepo,
            IReadRepository<ProjectFilePackage> packageRepo,
            IReadRepository<SharedProjectFile> sharedRepo,
            ICurrentUser currentUser)
        {
            this.projectFileRepo = projectFileRepo;
            this.packageRepo = packageRepo;
            this.sharedRepo = sharedRepo;
            this.currentUser = currentUser;
        }

        public async Task EnsureCanAccessFileAsync(
            Guid tenantId,
            Guid projectId,
            Guid fileId,
            FileAccessKind kind,
            CancellationToken cancellationToken)
        {
            ProjectFile? file = await projectFileRepo.GetFirstBySearch(
                f => f.Id == fileId
                    && f.TenantId == tenantId
                    && f.ProjectId == projectId,
                cancellationToken);

            if (file is null)
            {
                throw new NotFoundApiException(nameof(ProjectFile), fileId.ToString());
            }

            if (await currentUser.IsTenantOrProjectAdminAsync(tenantId, projectId, cancellationToken))
            {
                return;
            }

            if (file.OwnerId == currentUser.Id)
            {
                return;
            }

            if (kind == FileAccessKind.Read || kind == FileAccessKind.Write)
            {
                bool hasShareAccess = await sharedRepo.AnyAsync(
                    s => s.ProjectFileId == fileId
                        && s.SharedWithUserId == currentUser.Id,
                    cancellationToken);

                if (hasShareAccess)
                {
                    return;
                }
            }

            throw new ForbiddenApiException("You do not have access to this file.");
        }

        public async Task EnsureCanAccessPackageAsync(
            Guid tenantId,
            Guid projectId,
            Guid packageId,
            FileAccessKind kind,
            CancellationToken cancellationToken)
        {
            ProjectFilePackage? package = await packageRepo.GetFirstBySearch(
                p => p.Id == packageId
                    && p.TenantId == tenantId
                    && p.ProjectId == projectId,
                cancellationToken);

            if (package is null)
            {
                throw new NotFoundApiException(nameof(ProjectFilePackage), packageId.ToString());
            }

            if (await currentUser.IsTenantOrProjectAdminAsync(tenantId, projectId, cancellationToken))
            {
                return;
            }

            if (package.OwnerId == currentUser.Id)
            {
                return;
            }

            // Per existing handler semantics, package access (including upload to package, share package)
            // is restricted to admins and the package owner. Suppress unused-parameter warning by referencing kind.
            _ = kind;

            throw new ForbiddenApiException("You do not have access to this package.");
        }
    }
}
