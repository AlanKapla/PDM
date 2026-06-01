using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.Files;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.SharePackages
{
    /// <summary>
    /// Handler do udostępniania paczek członkom projektu
    /// Zawsze udostępnia CAŁE paczki (bez wykluczeń plików)
    /// </summary>
    public sealed class SharePackagesCommandHandler : IRequestHandler<SharePackagesCommand, Unit>
    {
        private readonly IRepository<SharedProjectFile> sharedProjectFileRepo;
        private readonly IRepository<ProjectFilePackage> packageRepo;
        private readonly IProjectFilesService projectFilesService;
        private readonly IFileAccessGuard fileAccessGuard;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<SharePackagesCommandHandler> logger;

        public SharePackagesCommandHandler(
            IRepository<SharedProjectFile> sharedProjectFileRepo,
            IRepository<ProjectFilePackage> packageRepo,
            IProjectFilesService projectFilesService,
            IFileAccessGuard fileAccessGuard,
            ICurrentUser currentUser,
            ILogger<SharePackagesCommandHandler> logger)
        {
            this.sharedProjectFileRepo = sharedProjectFileRepo;
            this.packageRepo = packageRepo;
            this.projectFilesService = projectFilesService;
            this.fileAccessGuard = fileAccessGuard;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(SharePackagesCommand request, CancellationToken cancellationToken)
        {
            // 1. Authorize each package (NotFound if missing, Forbidden if caller is not admin/owner)
            foreach (Guid packageId in request.PackageIds)
            {
                await fileAccessGuard.EnsureCanAccessPackageAsync(
                    request.TenantId, request.ProjectId, packageId, FileAccessKind.Share, cancellationToken);
            }

            // 2. Expand PackageIds with all descendants (cascade share)
            IReadOnlyList<ProjectFilePackage> allProjectPackages = await GetAllProjectPackagesAsync(
                request.TenantId, request.ProjectId);

            IReadOnlyList<Guid> allPackageIds = ExpandWithDescendants(request.PackageIds, allProjectPackages);

            // 3. Build owner lookup from already-fetched packages (no extra query)
            HashSet<Guid> allPackageIdsSet = new HashSet<Guid>(allPackageIds);
            Dictionary<Guid, Guid> packageOwners = allProjectPackages
                .Where(p => allPackageIdsSet.Contains(p.Id))
                .ToDictionary(p => p.Id, p => p.OwnerId);

            // 4. Share all packages (including descendants) with all specified users (excluding self and package owners)
            int totalShared = 0;
            int skippedSelf = 0;
            int skippedOwners = 0;

            foreach (Guid packageId in allPackageIds)
            {
                Guid packageOwnerId = packageOwners[packageId];

                foreach (Guid userId in request.SharedWithUserIds)
                {
                    // Pomijamy udostępnienie samemu sobie
                    if (userId == currentUser.Id)
                    {
                        skippedSelf++;
                        continue;
                    }

                    // Pomijamy udostępnienie właścicielowi paczki (owner już ma pełny dostęp)
                    if (userId == packageOwnerId)
                    {
                        skippedOwners++;
                        continue;
                    }

                    bool shared = await SharePackageWithUserAsync(request, packageId, userId, cancellationToken);
                    if (shared)
                    {
                        totalShared++;
                    }
                }
            }

            await sharedProjectFileRepo.SaveChangesAsync(cancellationToken);

            // Invalidate file access cache
            await projectFilesService.InvalidateFileAccessCacheAsync(request.TenantId, request.ProjectId, cancellationToken);

            logger.LogInformation(
                "{PackageCount} packages processed: {TotalShared} shares created, {SkippedSelf} skipped (self), {SkippedOwners} skipped (owners)",
                allPackageIds.Count, totalShared, skippedSelf, skippedOwners);

            return Unit.Value;
        }

        private async Task<IReadOnlyList<ProjectFilePackage>> GetAllProjectPackagesAsync(
            Guid tenantId, Guid projectId)
        {
            IEnumerable<ProjectFilePackage> packages = await packageRepo.GetBySearch(
                p => p.TenantId == tenantId && p.ProjectId == projectId);
            return packages.ToList();
        }

        private static IReadOnlyList<Guid> ExpandWithDescendants(
            IEnumerable<Guid> rootIds,
            IReadOnlyList<ProjectFilePackage> allPackages)
        {
            Dictionary<Guid, List<Guid>> childrenByParent = allPackages
                .Where(p => p.ParentId.HasValue)
                .GroupBy(p => p.ParentId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(p => p.Id).ToList());

            HashSet<Guid> result = new HashSet<Guid>();
            Queue<Guid> queue = new Queue<Guid>(rootIds);

            while (queue.Count > 0)
            {
                Guid current = queue.Dequeue();
                if (!result.Add(current)) continue;

                if (childrenByParent.TryGetValue(current, out List<Guid>? children))
                {
                    foreach (Guid child in children)
                    {
                        queue.Enqueue(child);
                    }
                }
            }

            return result.ToList();
        }

        /// <summary>
        /// Udostępnia pojedynczą paczkę pojedynczemu userowi
        /// - Usuwa wszystkie stare wpisy dla tej paczki z FileId
        /// - Dodaje wpis { PackageId, FileId: null, Access: Allow }
        /// </summary>
        /// <returns>True jeśli udostępnienie zostało dodane, False jeśli już istniało</returns>
        private async Task<bool> SharePackageWithUserAsync(
            SharePackagesCommand request,
            Guid packageId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            // Usuń wszystkie wpisy dla tej paczki z FileId (Allow i Deny) jednym zapytaniem SQL
            await sharedProjectFileRepo.ExecuteDeleteAsync(
                spf => spf.ProjectFilePackageId == packageId
                    && spf.ProjectFileId != null
                    && spf.SharedWithUserId == userId,
                cancellationToken);

            // Sprawdź czy paczka już jest udostępniona
            SharedProjectFile? existingPackageShare = await sharedProjectFileRepo.GetFirstBySearch(
                spf => spf.ProjectFilePackageId == packageId
                    && spf.ProjectFileId == null
                    && spf.SharedWithUserId == userId);

            if (existingPackageShare is null)
            {
                // Dodaj udostępnienie całej paczki
                SharedProjectFile packageShare = new SharedProjectFile
                {
                    TenantId = request.TenantId,
                    ProjectId = request.ProjectId,
                    ProjectFilePackageId = packageId,
                    ProjectFileId = null,  // Cała paczka
                    Access = ProjectFileAccess.Allow,
                    SharedByUserId = currentUser.Id,
                    SharedWithUserId = userId,
                    SharedAt = DateTime.UtcNow
                };

                await sharedProjectFileRepo.Insert(packageShare);
                
                logger.LogDebug(
                    "Package {PackageId} shared with user {UserId} by {CurrentUserId}",
                    packageId, userId, currentUser.Id);

                return true; // Nowe udostępnienie utworzone
            }

            logger.LogDebug(
                "Package {PackageId} already shared with user {UserId}",
                packageId, userId);

            return false; // Udostępnienie już istniało
        }
    }
}

