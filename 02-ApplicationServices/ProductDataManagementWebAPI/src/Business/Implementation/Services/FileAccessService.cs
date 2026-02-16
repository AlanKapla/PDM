using Business.Implementation.Services;
using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    /// <summary>
    /// Serwis sprawdzający dostęp do plików i paczek w modelu Package + Allow/Deny z cachowaniem per user i scope
    /// </summary>
    public sealed class FileAccessService : IFileAccessService
    {
        private readonly ICacheService cacheService;
        private readonly AccessService accessService;
        private readonly IReadRepository<SharedProjectFile> sharedFileRepository;
        private readonly IReadRepository<ProjectFile> fileRepository;
        private readonly IReadRepository<ProjectFilePackage> packageRepository;
        private readonly ILogger<FileAccessService> logger;

        public FileAccessService(
            ICacheService cacheService,
            AccessService accessService,
            IReadRepository<SharedProjectFile> sharedFileRepository,
            IReadRepository<ProjectFile> fileRepository,
            IReadRepository<ProjectFilePackage> packageRepository,
            ILogger<FileAccessService> logger)
        {
            this.cacheService = cacheService;
            this.accessService = accessService;
            this.sharedFileRepository = sharedFileRepository;
            this.fileRepository = fileRepository;
            this.packageRepository = packageRepository;
            this.logger = logger;
        }

        /// <summary>
        /// Zwraca IDs paczek do których user ma dostęp w projekcie zgodnie z ResourceScope
        /// </summary>
        public async Task<HashSet<Guid>> GetAccessiblePackageIdsAsync(
            ICurrentUser currentUser,
            Guid projectId,
            ResourceScope resourceScope,
            CancellationToken cancellationToken = default)
        {
            string cacheKey = $"file:access:packages:{currentUser.Id}:{projectId}:{resourceScope}";

            HashSet<Guid>? result = await cacheService.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    logger.LogDebug(
                        "Loading accessible package IDs for user {UserId}, project {ProjectId}, scope {Scope}",
                        currentUser.Id,
                        projectId,
                        resourceScope);

                    // Sprawdź uprawnienia do projektu
                    bool hasAccess = await CheckProjectAccessAsync(
                        currentUser,
                        projectId,
                        resourceScope,
                        cancellationToken);

                    if (!hasAccess)
                    {
                        logger.LogWarning(
                            "User {UserId} does not have access to project {ProjectId} with scope {Scope}",
                            currentUser.Id,
                            projectId,
                            resourceScope);
                        return new HashSet<Guid>();
                    }

                    HashSet<Guid> packageIds = new HashSet<Guid>();

                    if (resourceScope == ResourceScope.All)
                    {
                        // Pobierz wszystkie IDs paczek z projektu
                        List<Guid> allPackageIds = await packageRepository.GetIdsBySearchAsync(
                            p => p.ProjectId == projectId && 
                                 p.TenantId == currentUser.ActiveTenantId!.Value && 
                                 !p.IsDeleted,
                            cancellationToken);

                        packageIds = allPackageIds.ToHashSet();
                    }
                    else if (resourceScope == ResourceScope.Mine)
                    {
                        // Pobierz tylko IDs paczek należących do użytkownika
                        List<Guid> myPackageIds = await packageRepository.GetIdsBySearchAsync(
                            p => p.ProjectId == projectId && 
                                 p.TenantId == currentUser.ActiveTenantId!.Value && 
                                 p.OwnerId == currentUser.Id && 
                                 !p.IsDeleted,
                            cancellationToken);

                        packageIds = myPackageIds.ToHashSet();
                    }
                    else if (resourceScope == ResourceScope.Shared)
                    {
                        // Pobierz IDs paczek udostępnionych użytkownikowi
                        HashSet<Guid> sharedPackageIds = await sharedFileRepository.SelectToHashSetAsync(
                            spf => spf.ProjectId == projectId && 
                                   spf.SharedWithUserId == currentUser.Id,
                            spf => spf.ProjectFilePackageId,
                            cancellationToken);

                        packageIds = sharedPackageIds;
                    }

                    return packageIds;
                },
                expiration: TimeSpan.FromMinutes(15),
                cancellationToken: cancellationToken
            );

            return result ?? new HashSet<Guid>();
        }

        /// <summary>
        /// Zwraca słownik: PackageId -> Liczba dostępnych plików dla użytkownika zgodnie z ResourceScope
        /// </summary>
        public async Task<Dictionary<Guid, int>> GetAccessibleFileCountsAsync(
            ICurrentUser currentUser,
            HashSet<Guid> packageIds,
            ResourceScope resourceScope,
            CancellationToken cancellationToken = default)
        {
            if (!packageIds.Any())
            {
                return new Dictionary<Guid, int>();
            }

            string packageIdsKey = string.Join("-", packageIds.OrderBy(id => id));
            string cacheKey = $"file:access:counts:{currentUser.Id}:{packageIdsKey}:{resourceScope}";

            Dictionary<Guid, int>? result = await cacheService.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    logger.LogDebug(
                        "Loading accessible file counts for user {UserId}, {Count} packages, scope {Scope}",
                        currentUser.Id,
                        packageIds.Count,
                        resourceScope);

                    Dictionary<Guid, int> counts = new Dictionary<Guid, int>();

                    if (resourceScope == ResourceScope.All)
                    {
                        // Policz wszystkie pliki w każdej paczce
                        foreach (Guid packageId in packageIds)
                        {
                            int count = await fileRepository.CountAsync(
                                f => f.ProjectFilePackageId == packageId && !f.IsDeleted,
                                cancellationToken);
                            
                            counts[packageId] = count;
                        }
                    }
                    else if (resourceScope == ResourceScope.Mine)
                    {
                        // Policz tylko pliki należące do użytkownika
                        foreach (Guid packageId in packageIds)
                        {
                            int count = await fileRepository.CountAsync(
                                f => f.ProjectFilePackageId == packageId && 
                                     f.OwnerId == currentUser.Id && 
                                     !f.IsDeleted,
                                cancellationToken);
                            
                            counts[packageId] = count;
                        }
                    }
                    else if (resourceScope == ResourceScope.Shared)
                    {
                        // Pobierz shares dla tych paczek i użytkownika
                        IEnumerable<SharedProjectFile> shares = await sharedFileRepository.GetBySearch(
                            spf => packageIds.Contains(spf.ProjectFilePackageId) && 
                                   spf.SharedWithUserId == currentUser.Id);

                        ILookup<Guid, SharedProjectFile> sharesByPackage = shares.ToLookup(s => s.ProjectFilePackageId);

                        foreach (Guid packageId in packageIds)
                        {
                            List<SharedProjectFile> packageShares = sharesByPackage[packageId].ToList();

                            // Sprawdź czy paczka jest udostępniona
                            bool hasPackageShare = packageShares.Any(s => s.ProjectFileId == null);

                            if (hasPackageShare)
                            {
                                // Paczka udostępniona → policz wszystkie pliki OPRÓCZ wykluczeń
                                int totalFiles = await fileRepository.CountAsync(
                                    pf => pf.ProjectFilePackageId == packageId && !pf.IsDeleted,
                                    cancellationToken);

                                int excludedCount = packageShares.Count(s =>
                                    s.ProjectFileId.HasValue &&
                                    s.Access == ProjectFileAccess.Deny);

                                counts[packageId] = totalFiles - excludedCount;
                            }
                            else
                            {
                                // Paczka NIE udostępniona → policz tylko pliki z Allow
                                int allowedCount = packageShares.Count(s =>
                                    s.ProjectFileId.HasValue &&
                                    s.Access == ProjectFileAccess.Allow);

                                counts[packageId] = allowedCount;
                            }
                        }
                    }

                    return counts;
                },
                expiration: TimeSpan.FromMinutes(15),
                cancellationToken: cancellationToken
            );

            return result ?? new Dictionary<Guid, int>();
        }

        /// <summary>
        /// Zwraca informacje o dostępie do plików w paczce zgodnie z ResourceScope
        /// </summary>
        public async Task<PackageAccessInfo> GetPackageAccessInfoAsync(
            ICurrentUser currentUser,
            Guid packageId,
            ResourceScope resourceScope,
            CancellationToken cancellationToken = default)
        {
            string cacheKey = $"file:access:package:{currentUser.Id}:{packageId}:{resourceScope}";

            PackageAccessInfo? result = await cacheService.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    logger.LogDebug(
                        "Loading package access info for user {UserId}, package {PackageId}, scope {Scope}",
                        currentUser.Id,
                        packageId,
                        resourceScope);

                    if (resourceScope == ResourceScope.All)
                    {
                        // Wszystkie pliki dostępne
                        return new PackageAccessInfo
                        {
                            IsPackageShared = true,
                            ExcludedFileIds = new HashSet<Guid>(),
                            AllowedFileIds = new HashSet<Guid>()
                        };
                    }

                    if (resourceScope == ResourceScope.Mine)
                    {
                        // Zwróć IDs plików należących do użytkownika
                        List<Guid> myFileIds = await fileRepository.GetIdsBySearchAsync(
                            f => f.ProjectFilePackageId == packageId && 
                                 f.OwnerId == currentUser.Id && 
                                 !f.IsDeleted,
                            cancellationToken);

                        return new PackageAccessInfo
                        {
                            IsPackageShared = false,
                            ExcludedFileIds = new HashSet<Guid>(),
                            AllowedFileIds = myFileIds.ToHashSet()
                        };
                    }

                    // ResourceScope.Shared
                    IEnumerable<SharedProjectFile> shares = await sharedFileRepository.GetBySearch(
                        spf => spf.ProjectFilePackageId == packageId && 
                               spf.SharedWithUserId == currentUser.Id);

                    List<SharedProjectFile> sharesList = shares.ToList();
                    bool hasPackageAccess = sharesList.Any(s => s.ProjectFileId == null);

                    if (hasPackageAccess)
                    {
                        HashSet<Guid> excludedFileIds = sharesList
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
                        HashSet<Guid> allowedFileIds = sharesList
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
                },
                expiration: TimeSpan.FromMinutes(15),
                cancellationToken: cancellationToken
            );

            return result ?? new PackageAccessInfo
            {
                IsPackageShared = false,
                ExcludedFileIds = new HashSet<Guid>(),
                AllowedFileIds = new HashSet<Guid>()
            };
        }

        /// <summary>
        /// Sprawdza czy user ma dostęp do konkretnego pliku zgodnie z ResourceScope
        /// </summary>
        public async Task<bool> HasAccessToFileAsync(
            ICurrentUser currentUser,
            Guid packageId,
            Guid fileId,
            ResourceScope resourceScope,
            CancellationToken cancellationToken = default)
        {
            string cacheKey = $"file:access:file:{currentUser.Id}:{packageId}:{fileId}:{resourceScope}";

            BoolWrapper? result = await cacheService.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    logger.LogDebug(
                        "Checking file access for user {UserId}, package {PackageId}, file {FileId}, scope {Scope}",
                        currentUser.Id,
                        packageId,
                        fileId,
                        resourceScope);

                    bool hasAccess = false;

                    if (resourceScope == ResourceScope.All)
                    {
                        // Sprawdź czy plik istnieje i nie jest usunięty
                        hasAccess = await fileRepository.AnyAsync(
                            f => f.Id == fileId && f.ProjectFilePackageId == packageId && !f.IsDeleted,
                            cancellationToken);
                    }
                    else if (resourceScope == ResourceScope.Mine)
                    {
                        // Sprawdź czy plik należy do użytkownika
                        hasAccess = await fileRepository.AnyAsync(
                            f => f.Id == fileId && 
                                 f.ProjectFilePackageId == packageId && 
                                 f.OwnerId == currentUser.Id && 
                                 !f.IsDeleted,
                            cancellationToken);
                    }
                    else // ResourceScope.Shared
                    {
                        IEnumerable<SharedProjectFile> shares = await sharedFileRepository.GetBySearch(
                            spf => spf.ProjectFilePackageId == packageId && 
                                   spf.SharedWithUserId == currentUser.Id);

                        List<SharedProjectFile> sharesList = shares.ToList();
                        SharedProjectFile? packageShare = sharesList.FirstOrDefault(s => s.ProjectFileId == null);
                        SharedProjectFile? fileShare = sharesList.FirstOrDefault(s => s.ProjectFileId == fileId);

                        if (fileShare?.Access == ProjectFileAccess.Deny)
                        {
                            hasAccess = false;
                        }
                        else if (fileShare?.Access == ProjectFileAccess.Allow)
                        {
                            hasAccess = true;
                        }
                        else
                        {
                            hasAccess = packageShare != null;
                        }
                    }

                    return new BoolWrapper { Value = hasAccess };
                },
                expiration: TimeSpan.FromMinutes(15),
                cancellationToken: cancellationToken
            );

            return result?.Value ?? false;
        }

        /// <summary>
        /// Buduje słownik użytkowników mających dostęp do plików w paczce
        /// Uwzględnia Package + Allow/Deny model
        /// </summary>
        public async Task<Dictionary<Guid, List<Guid>>> GetSharedWithUsersAsync(
            Guid packageId,
            HashSet<Guid> fileIds,
            CancellationToken cancellationToken = default)
        {
            string fileIdsKey = string.Join("-", fileIds.OrderBy(id => id));
            string cacheKey = $"file:access:sharedwith:{packageId}:{fileIdsKey}";

            Dictionary<Guid, List<Guid>>? result = await cacheService.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    logger.LogDebug(
                        "Loading shared with users for package {PackageId}, {Count} files",
                        packageId,
                        fileIds.Count);

                    // Pobierz wszystkie udostępnienia dla paczki
                    IEnumerable<SharedProjectFile> allShares = await sharedFileRepository.GetBySearch(
                        spf => spf.ProjectFilePackageId == packageId);

                    // Grupuj po userId
                    IEnumerable<IGrouping<Guid, SharedProjectFile>> sharesByUser = allShares.GroupBy(s => s.SharedWithUserId);

                    Dictionary<Guid, List<Guid>> dictionary = new Dictionary<Guid, List<Guid>>();

                    foreach (Guid fileId in fileIds)
                    {
                        List<Guid> usersWithAccess = new List<Guid>();

                        foreach (IGrouping<Guid, SharedProjectFile> userShares in sharesByUser)
                        {
                            Guid userId = userShares.Key;

                            // Sprawdź czy user ma dostęp do tego pliku
                            SharedProjectFile? packageShare = userShares.FirstOrDefault(s => s.ProjectFileId == null);
                            SharedProjectFile? fileShare = userShares.FirstOrDefault(s => s.ProjectFileId == fileId);

                            bool hasAccess = false;

                            // Logika: (Package shared AND NIE Deny) OR Allow
                            if (fileShare?.Access == ProjectFileAccess.Deny)
                            {
                                hasAccess = false;  // Deny ma priorytet
                            }
                            else if (fileShare?.Access == ProjectFileAccess.Allow)
                            {
                                hasAccess = true;  // Jawny Allow
                            }
                            else if (packageShare != null)
                            {
                                hasAccess = true;  // Dostęp przez paczkę (i brak Deny)
                            }

                            if (hasAccess)
                            {
                                usersWithAccess.Add(userId);
                            }
                        }

                        if (usersWithAccess.Any())
                        {
                            dictionary[fileId] = usersWithAccess;
                        }
                    }

                    return dictionary;
                },
                expiration: TimeSpan.FromMinutes(15),
                cancellationToken: cancellationToken
            );

            return result ?? new Dictionary<Guid, List<Guid>>();
        }

        /// <summary>
        /// Sprawdza czy użytkownik ma dostęp do projektu zgodnie z podanym ResourceScope
        /// </summary>
        private async Task<bool> CheckProjectAccessAsync(
            ICurrentUser currentUser,
            Guid projectId,
            ResourceScope resourceScope,
            CancellationToken cancellationToken)
        {
            if (!currentUser.ActiveTenantId.HasValue)
            {
                logger.LogWarning("User {UserId} does not have active tenant", currentUser.Id);
                return false;
            }

            ResourceRef resource = new ResourceRef(
                TenantId: currentUser.ActiveTenantId.Value,
                ProjectId: projectId
            );

            string permissionCode = resourceScope switch
            {
                ResourceScope.All => PermissionCodes.ProjectResourcesReadAll,
                ResourceScope.Mine => PermissionCodes.ProjectResourcesRead,
                ResourceScope.Shared => PermissionCodes.ProjectResourcesReadShared,
                _ => PermissionCodes.ProjectResourcesRead
            };

            bool hasAccess = await accessService.AuthorizeAsync(
                currentUser,
                permissionCode,
                resource,
                resourceScope,
                cancellationToken);

            return hasAccess;
        }

        /// <summary>
        /// Wrapper class dla wartości bool do użycia w cache (wymóg: musi być klasą)
        /// </summary>
        private sealed class BoolWrapper
        {
            public bool Value { get; set; }
        }
    }
}




