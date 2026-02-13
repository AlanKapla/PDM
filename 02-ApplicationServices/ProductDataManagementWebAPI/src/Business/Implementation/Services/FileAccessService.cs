using Business.Interfaces.Services;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public class FileAccessService : IFileAccessService
    {
        private readonly IReadRepository<SharedProjectFile> sharedFileRepo;
        private readonly IRepository<ProjectFile> fileRepo;  // ✅ IRepository zamiast IReadRepository

        public FileAccessService(
            IReadRepository<SharedProjectFile> sharedFileRepo,
            IRepository<ProjectFile> fileRepo)
        {
            this.sharedFileRepo = sharedFileRepo;
            this.fileRepo = fileRepo;
        }

        public async Task<HashSet<Guid>> GetAccessiblePackageIdsAsync(
            Guid userId,
            Guid projectId,
            CancellationToken cancellationToken = default)
        {
            var shares = await sharedFileRepo.GetBySearch(
                spf => spf.ProjectId == projectId
                    && spf.SharedWithUserId == userId);

            return shares
                .Select(s => s.ProjectFilePackageId)
                .Distinct()
                .ToHashSet();
        }

        /// <summary>
        /// Zwraca słownik: PackageId -> Liczba dostępnych plików dla użytkownika
        /// Uwzględnia logikę Allow/Deny:
        /// - Paczka udostępniona (Package share) → wszystkie pliki OPRÓCZ wykluczeń (Deny)
        /// - Paczka NIE udostępniona → tylko pliki z jawnym Allow
        /// </summary>
        public async Task<Dictionary<Guid, int>> GetAccessibleFileCountsAsync(
            Guid userId,
            HashSet<Guid> packageIds,
            CancellationToken cancellationToken = default)
        {
            if (!packageIds.Any())
                return new Dictionary<Guid, int>();

            // Pobierz wszystkie shares dla tych paczek i tego usera
            var shares = await sharedFileRepo.GetBySearch(
                spf => packageIds.Contains(spf.ProjectFilePackageId)
                    && spf.SharedWithUserId == userId);

            var sharesByPackage = shares.GroupBy(s => s.ProjectFilePackageId);
            var result = new Dictionary<Guid, int>();

            foreach (var packageGroup in sharesByPackage)
            {
                var packageId = packageGroup.Key;
                var packageShares = packageGroup.ToList();

                // Sprawdź czy paczka jest udostępniona
                var hasPackageShare = packageShares.Any(s => s.ProjectFileId == null);

                if (hasPackageShare)
                {
                    // Paczka udostępniona → policz wszystkie pliki OPRÓCZ wykluczeń
                    var allFiles = await fileRepo.GetBySearch(
                        pf => pf.ProjectFilePackageId == packageId && !pf.IsDeleted);
                    
                    var totalFilesInPackage = allFiles.Count();

                    var excludedCount = packageShares.Count(s => 
                        s.ProjectFileId.HasValue && 
                        s.Access == ProjectFileAccess.Deny);

                    result[packageId] = totalFilesInPackage - excludedCount;
                }
                else
                {
                    // Paczka NIE udostępniona → policz tylko pliki z Allow
                    var allowedCount = packageShares.Count(s => 
                        s.ProjectFileId.HasValue && 
                        s.Access == ProjectFileAccess.Allow);

                    result[packageId] = allowedCount;
                }
            }

            return result;
        }

        public async Task<PackageAccessInfo> GetPackageAccessInfoAsync(
            Guid userId,
            Guid packageId,
            CancellationToken cancellationToken = default)
        {
            var shares = await sharedFileRepo.GetBySearch(
                spf => spf.ProjectFilePackageId == packageId
                    && spf.SharedWithUserId == userId);

            var hasPackageAccess = shares.Any(s => s.ProjectFileId == null);

            if (hasPackageAccess)
            {
                var excludedFileIds = shares
                    .Where(s => s.ProjectFileId.HasValue && s.Access == ProjectFileAccess.Deny)
                    .Select(s => s.ProjectFileId!.Value)
                    .ToHashSet();

                return new PackageAccessInfo
                {
                    IsPackageShared = true,
                    ExcludedFileIds = excludedFileIds,
                    AllowedFileIds = new HashSet<Guid>()
                };
            }
            else
            {
                var allowedFileIds = shares
                    .Where(s => s.ProjectFileId.HasValue && s.Access == ProjectFileAccess.Allow)
                    .Select(s => s.ProjectFileId!.Value)
                    .ToHashSet();

                return new PackageAccessInfo
                {
                    IsPackageShared = false,
                    ExcludedFileIds = new HashSet<Guid>(),
                    AllowedFileIds = allowedFileIds
                };
            }
        }

        public async Task<bool> HasAccessToFileAsync(
            Guid userId,
            Guid packageId,
            Guid fileId,
            CancellationToken cancellationToken = default)
        {
            var shares = await sharedFileRepo.GetBySearch(
                spf => spf.ProjectFilePackageId == packageId
                    && spf.SharedWithUserId == userId);

            var packageShare = shares.FirstOrDefault(s => s.ProjectFileId == null);
            var fileShare = shares.FirstOrDefault(s => s.ProjectFileId == fileId);

            if (fileShare?.Access == ProjectFileAccess.Deny)
            {
                return false;
            }

            if (fileShare?.Access == ProjectFileAccess.Allow)
            {
                return true;
            }

            return packageShare != null;
        }
    }
}




