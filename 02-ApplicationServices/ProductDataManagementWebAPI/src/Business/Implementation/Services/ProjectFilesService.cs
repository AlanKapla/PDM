using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Entities.Models;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services;

/// <summary>
/// Implementacja serwisu do zarządzania i cachowania danych plików projektów
/// </summary>
public sealed class ProjectFilesService : IProjectFilesService
{
    private readonly ICacheService cacheService;
    private readonly IReadRepository<ProjectFilePackage> packageRepository;
    private readonly IReadRepository<ProjectFile> fileRepository;
    private readonly IReadRepository<ProjectFileVersion> versionRepository;
    private readonly IReadRepository<ProjectFileVersionComment> commentRepository;
    private readonly ILogger<ProjectFilesService> logger;

    public ProjectFilesService(
        ICacheService cacheService,
        IReadRepository<ProjectFilePackage> packageRepository,
        IReadRepository<ProjectFile> fileRepository,
        IReadRepository<ProjectFileVersion> versionRepository,
        IReadRepository<ProjectFileVersionComment> commentRepository,
        ILogger<ProjectFilesService> logger)
    {
        this.cacheService = cacheService;
        this.packageRepository = packageRepository;
        this.fileRepository = fileRepository;
        this.versionRepository = versionRepository;
        this.commentRepository = commentRepository;
        this.logger = logger;
    }

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

        // Pobierz wszystkie wersje z cache
        Dictionary<Guid, List<ProjectFileVersionDto>> allVersionsByFile = await GetProjectFilesVersionsAsync(
            tenantId,
            projectId,
            cancellationToken);

        // Znajdź żądane wersje w cache
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
}



