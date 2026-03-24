using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.SharePackages
{
    /// <summary>
    /// Handler do udostępniania paczek członkom projektu
    /// Zawsze udostępnia CAŁE paczki (bez wykluczeń plików)
    /// </summary>
    public class SharePackagesCommandHandler : IRequestHandler<SharePackagesCommand, Unit>
    {
        private readonly IRepository<SharedProjectFile> sharedProjectFileRepo;
        private readonly IRepository<ProjectFilePackage> packageRepo;
        private readonly IProjectFilesService projectFilesService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<SharePackagesCommandHandler> logger;

        public SharePackagesCommandHandler(
            IRepository<SharedProjectFile> sharedProjectFileRepo,
            IRepository<ProjectFilePackage> packageRepo,
            IProjectFilesService projectFilesService,
            ICurrentUser currentUser,
            ILogger<SharePackagesCommandHandler> logger)
        {
            this.sharedProjectFileRepo = sharedProjectFileRepo;
            this.packageRepo = packageRepo;
            this.projectFilesService = projectFilesService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(SharePackagesCommand request, CancellationToken cancellationToken)
        {
            // 1. Verify all packages exist and authorize
            var packages = (await packageRepo.GetBySearch(
                p => request.PackageIds.Contains(p.Id)
                    && p.ProjectId == request.ProjectId
                    && p.TenantId == request.TenantId
                    && !p.IsDeleted)).ToList();

            if (packages.Count != request.PackageIds.Count)
            {
                throw new NotFoundApiException(nameof(ProjectFilePackage), "One or more packages not found");
            }

            // 2. Authorization check: tenant admin OR project admin OR owner of ALL packages
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(request.TenantId, request.ProjectId, cancellationToken);
            
            if (!isAdmin)
            {
                var notOwnedPackages = packages.Where(p => p.OwnerId != currentUser.Id).ToList();
                if (notOwnedPackages.Any())
                {
                    throw new ForbiddenApiException("You are not the owner of all selected packages");
                }
            }

            // 3. Create dictionary for fast owner lookup
            var packageOwners = packages.ToDictionary(p => p.Id, p => p.OwnerId);

            // 4. Share all packages with all specified users (excluding self and package owners)
            int totalShared = 0;
            int skippedSelf = 0;
            int skippedOwners = 0;

            foreach (var packageId in request.PackageIds)
            {
                var packageOwnerId = packageOwners[packageId];

                foreach (var userId in request.SharedWithUserIds)
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
                request.PackageIds.Count, totalShared, skippedSelf, skippedOwners);

            return Unit.Value;
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
            var existingPackageShare = await sharedProjectFileRepo.GetFirstBySearch(
                spf => spf.ProjectFilePackageId == packageId
                    && spf.ProjectFileId == null
                    && spf.SharedWithUserId == userId);

            if (existingPackageShare == null)
            {
                // Dodaj udostępnienie całej paczki
                var packageShare = new SharedProjectFile
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

