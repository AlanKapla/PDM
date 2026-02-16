using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Files;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;
using System.Collections.Concurrent;

namespace CQRS.Files.GetPackageFiles;

public class GetPackageFilesQueryHandler : IRequestHandler<GetPackageFilesQuery, List<ProjectFileWeb>>
{
    private readonly IProjectFilesService projectFilesService;
    private readonly IReadRepository<User> userRepository;
    private readonly ICurrentUser currentUser;

    public GetPackageFilesQueryHandler(
        IProjectFilesService projectFilesService,
        IReadRepository<User> userRepository,
        ICurrentUser currentUser)
    {
        this.projectFilesService = projectFilesService;
        this.userRepository = userRepository;
        this.currentUser = currentUser;
    }

    public async Task<List<ProjectFileWeb>> Handle(GetPackageFilesQuery request, CancellationToken cancellationToken)
    {
        // OPTIMIZATION 1: Fetch all cached data in PARALLEL instead of sequential
        var packagesTask = projectFilesService.GetProjectFilePackagesAsync(request.TenantId, request.ProjectId, cancellationToken);
        var filesTask = projectFilesService.GetProjectPackageFilesAsync(request.TenantId, request.ProjectId, cancellationToken);
        var allVersionsTask = projectFilesService.GetProjectFilesVersionsAsync(request.TenantId, request.ProjectId, cancellationToken);

        await Task.WhenAll(packagesTask, filesTask, allVersionsTask);

        var allPackages = await packagesTask;
        var allFilesByPackage = await filesTask;
        var allVersionsByFile = await allVersionsTask;

        if (!allPackages.TryGetValue(request.PackageId, out ProjectFilePackageDto? packageDto))
        {
            throw new NotFoundApiException(nameof(ProjectFilePackage), request.PackageId.ToString());
        }

        // Filter files based on scope
        List<ProjectFileCacheDto> accessibleFiles = await GetAccessibleFilesForScopeAsync(
            request.PackageId,
            request.TenantId,
            request.ProjectId,
            request.Scope,
            allFilesByPackage,
            cancellationToken);

        if (accessibleFiles.Count == 0)
        {
            return new List<ProjectFileWeb>();
        }

        // Sort by CreatedAt desc
        accessibleFiles.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));

        // OPTIMIZATION 2: Pre-calculate everything using direct lookups instead of LINQ
        int fileCount = accessibleFiles.Count;
        var versionCountDict = new Dictionary<Guid, int>(fileCount);
        var currentVersionIds = new HashSet<Guid>(fileCount);
        
        for (int i = 0; i < fileCount; i++)
        {
            var file = accessibleFiles[i];
            
            if (allVersionsByFile.TryGetValue(file.Id, out var versions))
            {
                versionCountDict[file.Id] = versions.Count;
                
                if (file.CurrentVersionId.HasValue)
                {
                    currentVersionIds.Add(file.CurrentVersionId.Value);
                }
            }
            else
            {
                versionCountDict[file.Id] = 0;
            }
        }

        // OPTIMIZATION 3: Fetch SAS URIs + sharing in PARALLEL
        var sasUrisTask = currentVersionIds.Count > 0
            ? projectFilesService.GetFileVersionsSasUrisAsync(request.TenantId, request.ProjectId, currentVersionIds.ToArray())
            : Task.FromResult(new Dictionary<Guid, FileVersionSasUriInfo>());

        var fileIds = accessibleFiles.Select(f => f.Id).ToHashSet();
        
        var sharingTask = (request.Scope == ResourceScope.Mine || request.Scope == ResourceScope.All)
            ? projectFilesService.GetSharedWithUsersAsync(request.PackageId, fileIds, cancellationToken)
            : Task.FromResult(new Dictionary<Guid, List<Guid>>());

        // Wait for SAS URIs and sharing
        await Task.WhenAll(sasUrisTask, sharingTask);

        var sasUrisDict = await sasUrisTask;
        var sharedWithDict = await sharingTask;

        // Collect user IDs from SAS URIs (they contain CreatedByUserId info via version data)
        var versionDataTask = currentVersionIds.Count > 0
            ? projectFilesService.GetVersionsByIdsAsync(request.TenantId, request.ProjectId, currentVersionIds, cancellationToken)
            : Task.FromResult(new ProjectFileVersionsResult());

        var versionsResult = await versionDataTask;

        // Collect ALL user IDs: version creators + file owners
        var allUserIds = new HashSet<Guid>(versionsResult.CreatedByUserIds);
        foreach (var file in accessibleFiles)
        {
            allUserIds.Add(file.OwnerId);
        }

        // Fetch ALL users at once
        var userDict = allUserIds.Count > 0
            ? await userRepository.GetDictionaryBySearchAsync(u => allUserIds.Contains(u.Id), cancellationToken)
            : new Dictionary<Guid, User>();

        bool isOwnerView = request.Scope == ResourceScope.Mine;

        // OPTIMIZATION 4: Increase parallelism for mapping
        var result = new ConcurrentBag<ProjectFileWeb>();

        await Parallel.ForEachAsync(
            accessibleFiles,
            new ParallelOptions 
            { 
                MaxDegreeOfParallelism = 20,
                CancellationToken = cancellationToken 
            },
            async (fileDto, ct) =>
            {
                var currentVersionDto = fileDto.CurrentVersionId.HasValue
                    ? versionsResult.Versions.GetValueOrDefault(fileDto.CurrentVersionId.Value)
                    : null;

                var totalVersions = versionCountDict.GetValueOrDefault(fileDto.Id, 0);

                var sharedWithUserIds = sharedWithDict.TryGetValue(fileDto.Id, out var shared)
                    ? shared
                    : new List<Guid>();

                var sasUris = fileDto.CurrentVersionId.HasValue
                    ? sasUrisDict.GetValueOrDefault(fileDto.CurrentVersionId.Value)
                    : null;

                var fileWeb = MapToProjectFileWeb(
                    fileDto,
                    packageDto.Name,
                    currentVersionDto,
                    userDict,
                    totalVersions,
                    isOwnerView,
                    sharedWithUserIds,
                    sasUris);

                result.Add(fileWeb);
                await Task.CompletedTask;
            });

        // Sort results by CreatedAt desc
        return result.OrderByDescending(f => f.CreatedAt).ToList();
    }

    private async Task<List<ProjectFileCacheDto>> GetAccessibleFilesForScopeAsync(
        Guid packageId,
        Guid tenantId,
        Guid projectId,
        ResourceScope scope,
        Dictionary<Guid, List<ProjectFileCacheDto>> allFilesByPackage,
        CancellationToken cancellationToken)
    {
        if (!allFilesByPackage.TryGetValue(packageId, out List<ProjectFileCacheDto>? packageFiles))
        {
            return new List<ProjectFileCacheDto>();
        }

        if (scope == ResourceScope.All)
        {
            return packageFiles;
        }

        if (scope == ResourceScope.Mine)
        {
            return packageFiles.Where(f => f.OwnerId == currentUser.Id).ToList();
        }

        // ResourceScope.Shared - use ProjectFilesService
        PackageAccessInfo accessInfo = await projectFilesService.GetPackageAccessInfoAsync(
            currentUser,
            packageId,
            scope,
            cancellationToken);

        if (accessInfo.IsPackageShared)
        {
            // Package shared - all files EXCEPT excluded
            return packageFiles.Where(f => !accessInfo.ExcludedFileIds.Contains(f.Id)).ToList();
        }
        else
        {
            // Package NOT shared - only allowed files
            return packageFiles.Where(f => accessInfo.AllowedFileIds.Contains(f.Id)).ToList();
        }
    }

    private ProjectFileWeb MapToProjectFileWeb(
        ProjectFileCacheDto fileDto,
        string packageName,
        ProjectFileVersionDto? currentVersionDto,
        Dictionary<Guid, User> userDict,
        int totalVersions,
        bool isOwnerView,
        List<Guid> sharedWithUserIds,
        FileVersionSasUriInfo? sasUris)
    {
        ProjectFileVersionWeb? currentVersionWeb = null;

        if (currentVersionDto != null && sasUris != null)
        {
            string createdByUserName = string.Empty;
            if (userDict.TryGetValue(currentVersionDto.CreatedByUserId, out User? user))
            {
                createdByUserName = $"{user.FirstName} {user.LastName}".Trim();
            }

            currentVersionWeb = new ProjectFileVersionWeb
            {
                Id = currentVersionDto.Id,
                ProjectFileId = currentVersionDto.ProjectFileId,
                VersionNumber = currentVersionDto.VersionNumber,
                ContentType = currentVersionDto.ContentType,
                FileSizeBytes = currentVersionDto.FileSizeBytes,
                CreatedAt = currentVersionDto.CreatedAt,
                CreatedByUserId = currentVersionDto.CreatedByUserId,
                CreatedByUserName = createdByUserName,
                SasUrlView = sasUris.SasUriView,
                SasUrlDownload = sasUris.SasUriDownload,
                Comments = new List<ProjectFileVersionCommentWeb>()
            };
        }

        return new ProjectFileWeb
        {
            Id = fileDto.Id,
            FileName = fileDto.FileName,
            DisplayName = fileDto.DisplayName,
            PackageName = packageName,
            CreatedAt = fileDto.CreatedAt,
            OwnerId = fileDto.OwnerId,
            OwnerName = userDict.TryGetValue(fileDto.OwnerId, out User? owner)
                ? $"{owner.FirstName} {owner.LastName}".Trim()
                : string.Empty,
            CurrentVersion = currentVersionWeb,
            Versions = new List<ProjectFileVersionWeb>(),
            TotalVersions = totalVersions,
            IsOwner = isOwnerView && fileDto.OwnerId == currentUser.Id,
            IsShared = sharedWithUserIds.Any(),
            SharedWithUserIds = sharedWithUserIds
        };
    }
}
