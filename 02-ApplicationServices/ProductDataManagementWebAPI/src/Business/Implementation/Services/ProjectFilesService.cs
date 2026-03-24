using Business.Interfaces.Configurations;
using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services;

/// <summary>
/// Zintegrowany serwis zarządzający dostępem i cachowaniem danych plików projektów
/// </summary>
public sealed class ProjectFilesService : IProjectFilesService
{
    private readonly ICacheService cacheService;
    private readonly AccessService accessService;
    private readonly IReadRepository<SharedProjectFile> sharedFileRepository;
    private readonly IReadRepository<ProjectFile> fileRepository;
    private readonly IReadRepository<ProjectFilePackage> packageRepository;
    private readonly IReadRepository<ProjectFileVersion> versionRepository;
    private readonly IReadRepository<ProjectFileVersionComment> commentRepository;
    private readonly IBlobStorageService blobStorageService;
    private readonly ILogger<ProjectFilesService> logger;

    public ProjectFilesService(
        ICacheService cacheService,
        AccessService accessService,
        IReadRepository<SharedProjectFile> sharedFileRepository,
        IReadRepository<ProjectFile> fileRepository,
        IReadRepository<ProjectFilePackage> packageRepository,
        IReadRepository<ProjectFileVersion> versionRepository,
        IReadRepository<ProjectFileVersionComment> commentRepository,
        IBlobStorageService blobStorageService,
        ILogger<ProjectFilesService> logger)
    {
        this.cacheService = cacheService;
        this.accessService = accessService;
        this.sharedFileRepository = sharedFileRepository;
        this.fileRepository = fileRepository;
        this.packageRepository = packageRepository;
        this.versionRepository = versionRepository;
        this.commentRepository = commentRepository;
        this.blobStorageService = blobStorageService;
        this.logger = logger;
    }

    #region File Access Methods

    /// <summary>
    /// Zwraca IDs paczek do których user ma dostęp w projekcie zgodnie z ResourceScope
    /// </summary>
    public async Task<HashSet<Guid>> GetAccessiblePackageIdsAsync(
        ICurrentUser currentUser,
        Guid tenantId,
        Guid projectId,
        ResourceScope resourceScope,
        CancellationToken cancellationToken = default)
    {
        string cacheKey = $"file:access:{tenantId}:{projectId}:packages:{currentUser.Id}:{resourceScope}";

        HashSet<Guid>? result = await cacheService.GetOrAddAsync(
            cacheKey,
            async () =>
            {
                logger.LogDebug(
                    "Loading accessible package IDs for user {UserId}, project {ProjectId}, scope {Scope}",
                    currentUser.Id,
                    projectId,
                    resourceScope);

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
                    List<Guid> allPackageIds = await packageRepository.GetIdsBySearchAsync(
                        p => p.ProjectId == projectId && 
                             p.TenantId == tenantId && 
                             !p.IsDeleted,
                        cancellationToken);

                    packageIds = allPackageIds.ToHashSet();
                }
                else if (resourceScope == ResourceScope.Mine)
                {
                    List<Guid> myPackageIds = await packageRepository.GetIdsBySearchAsync(
                        p => p.ProjectId == projectId && 
                             p.TenantId == tenantId && 
                             p.OwnerId == currentUser.Id && 
                             !p.IsDeleted,
                        cancellationToken);

                    packageIds = myPackageIds.ToHashSet();
                }
                else if (resourceScope == ResourceScope.Shared)
                {
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
        Guid tenantId,
        Guid projectId,
        HashSet<Guid> packageIds,
        ResourceScope resourceScope,
        CancellationToken cancellationToken = default)
    {
        if (!packageIds.Any())
        {
            return new Dictionary<Guid, int>();
        }

        string packageIdsKey = string.Join("-", packageIds.OrderBy(id => id));
        string cacheKey = $"file:access:{tenantId}:{projectId}:counts:{currentUser.Id}:{packageIdsKey}:{resourceScope}";

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
                    IEnumerable<SharedProjectFile> shares = await sharedFileRepository.GetBySearch(
                        spf => packageIds.Contains(spf.ProjectFilePackageId) && 
                               spf.SharedWithUserId == currentUser.Id);

                    ILookup<Guid, SharedProjectFile> sharesByPackage = shares.ToLookup(s => s.ProjectFilePackageId);

                    foreach (Guid packageId in packageIds)
                    {
                        List<SharedProjectFile> packageShares = sharesByPackage[packageId].ToList();

                        bool hasPackageShare = packageShares.Any(s => s.ProjectFileId == null);

                        if (hasPackageShare)
                        {
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
        Guid tenantId,
        Guid projectId,
        Guid packageId,
        ResourceScope resourceScope,
        CancellationToken cancellationToken = default)
    {
        string cacheKey = $"file:access:{tenantId}:{projectId}:package:{currentUser.Id}:{packageId}:{resourceScope}";

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
                    return new PackageAccessInfo
                    {
                        IsPackageShared = true,
                        ExcludedFileIds = new HashSet<Guid>(),
                        AllowedFileIds = new HashSet<Guid>()
                    };
                }

                if (resourceScope == ResourceScope.Mine)
                {
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

                IEnumerable<SharedProjectFile> shares = await sharedFileRepository.GetBySearch(
                    spf => spf.ProjectFilePackageId == packageId && 
                           spf.ProjectId == projectId &&
                           spf.TenantId == tenantId &&
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
        Guid tenantId,
        Guid projectId,
        Guid packageId,
        Guid fileId,
        ResourceScope resourceScope,
        CancellationToken cancellationToken = default)
    {
        string cacheKey = $"file:access:{tenantId}:{projectId}:file:{currentUser.Id}:{packageId}:{fileId}:{resourceScope}";

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
                    hasAccess = await fileRepository.AnyAsync(
                        f => f.Id == fileId && f.ProjectFilePackageId == packageId && !f.IsDeleted,
                        cancellationToken);
                }
                else if (resourceScope == ResourceScope.Mine)
                {
                    hasAccess = await fileRepository.AnyAsync(
                        f => f.Id == fileId && 
                             f.ProjectFilePackageId == packageId && 
                             f.OwnerId == currentUser.Id && 
                             !f.IsDeleted,
                        cancellationToken);
                }
                else
                {
                    IEnumerable<SharedProjectFile> shares = await sharedFileRepository.GetBySearch(
                        spf => spf.ProjectFilePackageId == packageId && 
                               spf.ProjectId == projectId &&
                               spf.TenantId == tenantId &&
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
        Guid tenantId,
        Guid projectId,
        Guid packageId,
        HashSet<Guid> fileIds,
        CancellationToken cancellationToken = default)
    {
        string fileIdsKey = string.Join("-", fileIds.OrderBy(id => id));
        string cacheKey = $"file:access:{tenantId}:{projectId}:sharedwith:{packageId}:{fileIdsKey}";

        Dictionary<Guid, List<Guid>>? result = await cacheService.GetOrAddAsync(
            cacheKey,
            async () =>
            {
                logger.LogDebug(
                    "Loading shared with users for package {PackageId}, {Count} files",
                    packageId,
                    fileIds.Count);

                IEnumerable<SharedProjectFile> allShares = await sharedFileRepository.GetBySearch(
                    spf => spf.ProjectFilePackageId == packageId &&
                           spf.ProjectId == projectId &&
                           spf.TenantId == tenantId);

                IEnumerable<IGrouping<Guid, SharedProjectFile>> sharesByUser = allShares.GroupBy(s => s.SharedWithUserId);

                Dictionary<Guid, List<Guid>> dictionary = new Dictionary<Guid, List<Guid>>();

                foreach (Guid fileId in fileIds)
                {
                    List<Guid> usersWithAccess = new List<Guid>();

                    foreach (IGrouping<Guid, SharedProjectFile> userShares in sharesByUser)
                    {
                        Guid userId = userShares.Key;

                        SharedProjectFile? packageShare = userShares.FirstOrDefault(s => s.ProjectFileId == null);
                        SharedProjectFile? fileShare = userShares.FirstOrDefault(s => s.ProjectFileId == fileId);

                        bool hasAccess = false;

                        if (fileShare?.Access == ProjectFileAccess.Deny)
                        {
                            hasAccess = false;
                        }
                        else if (fileShare?.Access == ProjectFileAccess.Allow)
                        {
                            hasAccess = true;
                        }
                        else if (packageShare != null)
                        {
                            hasAccess = true;
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

    #endregion

    #region File Data Methods

    /// <summary>
    /// Pobiera wszystkie paczki plików dla projektu jako słownik [PackageId -> PackageDto]
    /// </summary>
    public async Task<Dictionary<Guid, ProjectFilePackageDto>> GetProjectFilePackagesAsync(
        Guid tenantId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        string cacheKey = $"project:files:packages:{tenantId}:{projectId}";

        Dictionary<Guid, ProjectFilePackageDto>? result = await cacheService.GetOrAddAsync(
            cacheKey,
            async () =>
            {
                logger.LogDebug("Loading file packages for project {ProjectId} from database", projectId);

                IEnumerable<ProjectFilePackage> packages = await packageRepository.GetBySearch(
                    p => p.TenantId == tenantId && p.ProjectId == projectId && !p.IsDeleted);

                List<ProjectFilePackage> sortedPackages = packages
                    .OrderBy(p => p.CreatedAt)
                    .ToList();

                Dictionary<Guid, ProjectFilePackageDto> dictionary = sortedPackages.ToDictionary(
                    p => p.Id,
                    p => new ProjectFilePackageDto
                    {
                        Id = p.Id,
                        TenantId = p.TenantId,
                        ProjectId = p.ProjectId,
                        OwnerId = p.OwnerId,
                        Name = p.Name,
                        CreatedAt = p.CreatedAt,
                        CreatedByUserId = p.CreatedByUserId,
                        IsDeleted = p.IsDeleted
                    });

                return dictionary;
            },
            expiration: TimeSpan.FromMinutes(30),
            cancellationToken: cancellationToken
        );

        return result ?? new Dictionary<Guid, ProjectFilePackageDto>();
    }

    /// <summary>
    /// Pobiera wszystkie pliki dla projektu pogrupowane według paczek jako słownik [PackageId -> Lista plików]
    /// </summary>
    public async Task<Dictionary<Guid, List<ProjectFileCacheDto>>> GetProjectPackageFilesAsync(
        Guid tenantId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        string cacheKey = $"project:files:files:{tenantId}:{projectId}";

        Dictionary<Guid, List<ProjectFileCacheDto>>? result = await cacheService.GetOrAddAsync(
            cacheKey,
            async () =>
            {
                logger.LogDebug("Loading files for project {ProjectId} from database", projectId);

                IEnumerable<ProjectFile> files = await fileRepository.GetBySearch(
                    f => f.TenantId == tenantId && f.ProjectId == projectId && !f.IsDeleted);

                List<ProjectFile> sortedFiles = files
                    .OrderBy(f => f.CreatedAt)
                    .ToList();

                Dictionary<Guid, List<ProjectFileCacheDto>> dictionary = sortedFiles
                    .GroupBy(f => f.ProjectFilePackageId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(f => new ProjectFileCacheDto
                        {
                            Id = f.Id,
                            TenantId = f.TenantId,
                            ProjectId = f.ProjectId,
                            ProjectFilePackageId = f.ProjectFilePackageId,
                            OwnerId = f.OwnerId,
                            FileName = f.FileName,
                            DisplayName = f.DisplayName,
                            CreatedAt = f.CreatedAt,
                            CurrentVersionId = f.CurrentVersionId,
                            IsDeleted = f.IsDeleted
                        }).ToList());

                return dictionary;
            },
            expiration: TimeSpan.FromMinutes(30),
            cancellationToken: cancellationToken
        );

        return result ?? new Dictionary<Guid, List<ProjectFileCacheDto>>();
    }

    /// <summary>
    /// Pobiera wszystkie wersje plików dla projektu pogrupowane według plików jako słownik [FileId -> Lista wersji]
    /// </summary>
    public async Task<Dictionary<Guid, List<ProjectFileVersionDto>>> GetProjectFilesVersionsAsync(
        Guid tenantId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        string cacheKey = $"project:files:versions:{tenantId}:{projectId}";

        Dictionary<Guid, List<ProjectFileVersionDto>>? result = await cacheService.GetOrAddAsync(
            cacheKey,
            async () =>
            {
                logger.LogDebug("Loading file versions for project {ProjectId} from database", projectId);

                List<Guid> projectFileIds = await fileRepository.GetIdsBySearchAsync(
                    f => f.TenantId == tenantId && f.ProjectId == projectId && !f.IsDeleted,
                    cancellationToken);

                if (projectFileIds.Count == 0)
                {
                    return new Dictionary<Guid, List<ProjectFileVersionDto>>();
                }

                IEnumerable<ProjectFileVersion> versions = await versionRepository.GetBySearch(
                    v => projectFileIds.Contains(v.ProjectFileId) && !v.IsDeleted);

                List<ProjectFileVersion> sortedVersions = versions
                    .OrderBy(v => v.CreatedAt)
                    .ToList();

                Dictionary<Guid, List<ProjectFileVersionDto>> dictionary = sortedVersions
                    .GroupBy(v => v.ProjectFileId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(v => new ProjectFileVersionDto
                        {
                            Id = v.Id,
                            ProjectFileId = v.ProjectFileId,
                            VersionNumber = v.VersionNumber,
                            CreatedByUserId = v.CreatedByUserId,
                            BlobFileName = v.BlobFileName,
                            BlobPath = v.BlobPath,
                            ContentType = v.ContentType,
                            FileSizeBytes = v.FileSizeBytes,
                            CreatedAt = v.CreatedAt,
                            IsDeleted = v.IsDeleted
                        }).ToList());

                return dictionary;
            },
            expiration: TimeSpan.FromMinutes(30),
            cancellationToken: cancellationToken
        );

        return result ?? new Dictionary<Guid, List<ProjectFileVersionDto>>();
    }

    /// <summary>
    /// Pobiera wszystkie komentarze dla wersji plików projektu pogrupowane według wersji jako słownik [VersionId -> Lista komentarzy]
    /// </summary>
    public async Task<Dictionary<Guid, List<ProjectFileVersionCommentDto>>> GetProjectFileVersionsCommentsAsync(
        Guid tenantId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        string cacheKey = $"project:files:comments:{tenantId}:{projectId}";

        Dictionary<Guid, List<ProjectFileVersionCommentDto>>? result = await cacheService.GetOrAddAsync(
            cacheKey,
            async () =>
            {
                logger.LogDebug("Loading file version comments for project {ProjectId} from database", projectId);

                IEnumerable<ProjectFileVersionComment> comments = await commentRepository.GetBySearch(
                    c => c.TenantId == tenantId && c.ProjectId == projectId && !c.IsDeleted);

                List<ProjectFileVersionComment> sortedComments = comments
                    .OrderBy(c => c.CreatedAt)
                    .ToList();

                Dictionary<Guid, List<ProjectFileVersionCommentDto>> dictionary = sortedComments
                    .GroupBy(c => c.ProjectFileVersionId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(c => new ProjectFileVersionCommentDto
                        {
                            Id = c.Id,
                            ProjectFileVersionId = c.ProjectFileVersionId,
                            ProjectId = c.ProjectId,
                            TenantId = c.TenantId,
                            UserId = c.UserId,
                            Content = c.Content,
                            CreatedAt = c.CreatedAt,
                            EditedAt = c.EditedAt,
                            IsDeleted = c.IsDeleted
                        }).ToList());

                return dictionary;
            },
            expiration: TimeSpan.FromMinutes(30),
            cancellationToken: cancellationToken
        );

        return result ?? new Dictionary<Guid, List<ProjectFileVersionCommentDto>>();
    }

    /// <summary>
    /// Pobiera wybrane wersje plików z cache
    /// </summary>
    public async Task<ProjectFileVersionsResult> GetVersionsByIdsAsync(
        Guid tenantId,
        Guid projectId,
        HashSet<Guid> versionIds,
        CancellationToken cancellationToken = default)
    {
        if (!versionIds.Any())
        {
            return new ProjectFileVersionsResult();
        }

        logger.LogDebug(
            "Loading {Count} versions for project {ProjectId}",
            versionIds.Count,
            projectId);

        Dictionary<Guid, List<ProjectFileVersionDto>> allVersionsByFile = await GetProjectFilesVersionsAsync(
            tenantId,
            projectId,
            cancellationToken);

        Dictionary<Guid, ProjectFileVersionDto> versionDict = new Dictionary<Guid, ProjectFileVersionDto>();
        HashSet<Guid> createdByUserIds = new HashSet<Guid>();

        foreach (List<ProjectFileVersionDto> versions in allVersionsByFile.Values)
        {
            foreach (ProjectFileVersionDto version in versions)
            {
                if (versionIds.Contains(version.Id))
                {
                    versionDict[version.Id] = version;
                    createdByUserIds.Add(version.CreatedByUserId);
                }
            }
        }

        return new ProjectFileVersionsResult
        {
            Versions = versionDict,
            CreatedByUserIds = createdByUserIds
        };
    }

    /// <summary>
    /// Pobiera SAS URI dla wielu wersji plików jednocześnie z cachowaniem
    /// TTL: 5 minut krótsze niż User Delegation Key (cache per VersionId, pobieranie MGET)
    /// </summary>
    public async Task<Dictionary<Guid, FileVersionSasUriInfo>> GetFileVersionsSasUrisAsync(
        Guid tenantId,
        Guid projectId,
        params Guid[] versionIds)
    {
        if (versionIds.Length == 0)
        {
            return new Dictionary<Guid, FileVersionSasUriInfo>();
        }

        logger.LogDebug(
            "Loading SAS URIs for {Count} file versions in project {ProjectId}",
            versionIds.Length,
            projectId);

        Dictionary<Guid, List<ProjectFileVersionDto>> allVersionsByFile = await GetProjectFilesVersionsAsync(
            tenantId,
            projectId,
            CancellationToken.None);

        Dictionary<Guid, ProjectFileCacheDto> allFilesById = (await GetProjectPackageFilesAsync(
            tenantId,
            projectId,
            CancellationToken.None))
            .SelectMany(kvp => kvp.Value)
            .ToDictionary(f => f.Id);

        List<string> cacheKeys = versionIds.Select(id => $"fileversion:sas:{id}").ToList();

        Dictionary<string, FileVersionSasUriInfo> cachedResults = await cacheService.GetManyAsync<FileVersionSasUriInfo>(
            cacheKeys,
            CancellationToken.None);

        Dictionary<Guid, FileVersionSasUriInfo> results = new Dictionary<Guid, FileVersionSasUriInfo>();
        List<Guid> missingVersionIds = new List<Guid>();

        for (int i = 0; i < versionIds.Length; i++)
        {
            Guid versionId = versionIds[i];
            string cacheKey = cacheKeys[i];

            if (cachedResults.TryGetValue(cacheKey, out FileVersionSasUriInfo? cached))
            {
                if (cached.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(5))
                {
                    results[versionId] = cached;
                }
                else
                {
                    missingVersionIds.Add(versionId);
                }
            }
            else
            {
                missingVersionIds.Add(versionId);
            }
        }

        if (missingVersionIds.Count > 0)
        {
            Dictionary<string, FileVersionSasUriInfo> newEntries = new Dictionary<string, FileVersionSasUriInfo>();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset expiresOn = NormalizeToBlock(now.AddMinutes(60), minutes: 15);
            DateTimeOffset sasExpiresAt = expiresOn.AddMinutes(-5);

            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.Documentation);

            foreach (Guid versionId in missingVersionIds)
            {
                ProjectFileVersionDto? versionDto = allVersionsByFile.Values
                    .SelectMany(v => v)
                    .FirstOrDefault(v => v.Id == versionId);

                if (versionDto == null)
                {
                    logger.LogWarning("Version {VersionId} not found in cache", versionId);
                    continue;
                }

                if (!allFilesById.TryGetValue(versionDto.ProjectFileId, out ProjectFileCacheDto? fileDto))
                {
                    logger.LogWarning("File {FileId} not found for version {VersionId}", versionDto.ProjectFileId, versionId);
                    continue;
                }

                string extension = Path.GetExtension(fileDto.FileName);
                string displayNameWithExtension = $"{fileDto.DisplayName}{extension}";

                Uri sasUriView = blobStorageService.GenerateSasUri(
                    containerName,
                    versionDto.BlobPath,
                    displayNameWithExtension,
                    expiresInMinutes: 60,
                    contentDisposition: "inline");

                Uri sasUriDownload = blobStorageService.GenerateSasUri(
                    containerName,
                    versionDto.BlobPath,
                    displayNameWithExtension,
                    expiresInMinutes: 60,
                    contentDisposition: "attachment");

                FileVersionSasUriInfo sasInfo = new FileVersionSasUriInfo
                {
                    VersionId = versionId,
                    SasUriView = sasUriView.ToString(),
                    SasUriDownload = sasUriDownload.ToString(),
                    ExpiresAt = sasExpiresAt
                };

                results[versionId] = sasInfo;
                newEntries[$"file:sas:{versionId}"] = sasInfo;
            }

            if (newEntries.Count > 0)
            {
                TimeSpan cacheExpiration = sasExpiresAt - now;
                await cacheService.SetManyAsync(newEntries, cacheExpiration, CancellationToken.None);

                logger.LogDebug(
                    "Cached {Count} new SAS URIs with expiration {Expiration}",
                    newEntries.Count,
                    cacheExpiration);
            }
        }

        return results;
    }

    #endregion

    #region Cache Invalidation Methods

    /// <summary>
    /// Invaliduje cache plików, paczek i wersji dla projektu
    /// </summary>
    public async Task InvalidateProjectFilesCacheAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken = default)
    {
        string packagesCacheKey = $"project:files:packages:{tenantId}:{projectId}";
        string filesCacheKey = $"project:files:files:{tenantId}:{projectId}";

        await cacheService.RemoveCacheByKeyAsync(packagesCacheKey, cancellationToken);
        await cacheService.RemoveCacheByKeyAsync(filesCacheKey, cancellationToken);

        logger.LogDebug("Invalidated files and packages cache for project {ProjectId}", projectId);
    }

    /// <summary>
    /// Invaliduje cache wersji plików dla projektu
    /// </summary>
    public async Task InvalidateProjectVersionsCacheAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken = default)
    {
        string versionsCacheKey = $"project:files:versions:{tenantId}:{projectId}";
        await cacheService.RemoveCacheByKeyAsync(versionsCacheKey, cancellationToken);

        logger.LogDebug("Invalidated versions cache for project {ProjectId}", projectId);
    }

    /// <summary>
    /// Invaliduje cache komentarzy dla projektu
    /// </summary>
    public async Task InvalidateProjectCommentsCacheAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken = default)
    {
        string commentsCacheKey = $"project:files:comments:{tenantId}:{projectId}";
        await cacheService.RemoveCacheByKeyAsync(commentsCacheKey, cancellationToken);

        logger.LogDebug("Invalidated comments cache for project {ProjectId}", projectId);
    }

    /// <summary>
    /// Invaliduje cache dostępu do plików dla projektu
    /// </summary>
    public async Task InvalidateFileAccessCacheAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken = default)
    {
        await cacheService.RemoveCacheContainsAsync($"file:access:{tenantId}:{projectId}:*", cancellationToken);

        logger.LogDebug("Invalidated file access cache for tenant {TenantId}, project {ProjectId}", tenantId, projectId);
    }

    /// <summary>
    /// Invaliduje cache SAS URI dla konkretnej wersji pliku
    /// </summary>
    public async Task InvalidateVersionSasUriAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        string sasUriCacheKey = $"fileversion:sas:{versionId}";
        await cacheService.RemoveCacheByKeyAsync(sasUriCacheKey, cancellationToken);

        logger.LogDebug("Invalidated SAS URI cache for version {VersionId}", versionId);
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Normalizuje DateTimeOffset do bloków (np. 15 minut) aby maksymalizować cache hits
    /// </summary>
    private static DateTimeOffset NormalizeToBlock(DateTimeOffset dateTime, int minutes)
    {
        int totalMinutes = dateTime.Minute + (dateTime.Hour * 60);
        int normalizedMinutes = (int)Math.Ceiling(totalMinutes / (double)minutes) * minutes;
        
        int hours = normalizedMinutes / 60;
        int mins = normalizedMinutes % 60;
        
        return new DateTimeOffset(
            dateTime.Year,
            dateTime.Month,
            dateTime.Day,
            hours,
            mins,
            0,
            dateTime.Offset);
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

    #endregion
}
