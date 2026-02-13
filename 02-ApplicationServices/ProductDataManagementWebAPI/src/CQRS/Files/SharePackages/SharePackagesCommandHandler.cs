using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
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
        private readonly ICurrentUser currentUser;
        private readonly ILogger<SharePackagesCommandHandler> logger;

        public SharePackagesCommandHandler(
            IRepository<SharedProjectFile> sharedProjectFileRepo,
            IRepository<ProjectFilePackage> packageRepo,
            ICurrentUser currentUser,
            ILogger<SharePackagesCommandHandler> logger)
        {
            this.sharedProjectFileRepo = sharedProjectFileRepo;
            this.packageRepo = packageRepo;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(SharePackagesCommand request, CancellationToken cancellationToken)
        {
            // 1. Verify all packages exist and authorize
            var packages = await packageRepo.GetBySearch(
                p => request.PackageIds.Contains(p.Id)
                    && p.ProjectId == request.ProjectId
                    && p.TenantId == request.TenantId
                    && !p.IsDeleted);

            if (packages.Count() != request.PackageIds.Count)
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

            // 3. Share all packages with all specified users
            foreach (var packageId in request.PackageIds)
            {
                foreach (var userId in request.SharedWithUserIds)
                {
                    await SharePackageWithUserAsync(request, packageId, userId, cancellationToken);
                }
            }

            await sharedProjectFileRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "{PackageCount} packages shared with {UserCount} users by {CurrentUserId}",
                request.PackageIds.Count, request.SharedWithUserIds.Count, currentUser.Id);

            return Unit.Value;
        }

        /// <summary>
        /// Udostępnia pojedynczą paczkę pojedynczemu userowi
        /// - Usuwa wszystkie stare wpisy dla tej paczki z FileId
        /// - Dodaje wpis { PackageId, FileId: null, Access: Allow }
        /// </summary>
        private async Task SharePackageWithUserAsync(
            SharePackagesCommand request,
            Guid packageId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            // Usuń wszystkie wpisy dla tej paczki z FileId (Allow i Deny)
            var existingFileShares = await sharedProjectFileRepo.GetBySearch(
                spf => spf.ProjectFilePackageId == packageId
                    && spf.ProjectFileId != null
                    && spf.SharedWithUserId == userId);

            if (existingFileShares.Any())
            {
                await sharedProjectFileRepo.DeleteRange(existingFileShares);
            }

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
                
                logger.LogInformation(
                    "Package {PackageId} shared entirely with user {UserId} by {CurrentUserId}",
                    packageId, userId, currentUser.Id);
            }
        }
    }
}

