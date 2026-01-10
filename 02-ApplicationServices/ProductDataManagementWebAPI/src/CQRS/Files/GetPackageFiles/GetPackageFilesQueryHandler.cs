using Business.Interfaces.Configurations;
using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Files;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.GetPackageFiles;

public class GetPackageFilesQueryHandler : IRequestHandler<GetPackageFilesQuery, List<ProjectFileWeb>>
{
    private readonly IRepository<ProjectFilePackage> packageRepo;
    private readonly IRepository<ProjectFile> fileRepo;
    private readonly IRepository<ProjectFileVersion> versionRepo;
    private readonly IRepository<SharedProjectFile> sharedProjectFileRepo;
    private readonly ICurrentUser currentUser;
    private readonly IBlobStorageService blobStorageService;

    public GetPackageFilesQueryHandler(
        IRepository<ProjectFilePackage> packageRepo,
        IRepository<ProjectFile> fileRepo,
        IRepository<ProjectFileVersion> versionRepo,
        IRepository<SharedProjectFile> sharedProjectFileRepo,
        ICurrentUser currentUser,
        IBlobStorageService blobStorageService)
    {
        this.packageRepo = packageRepo;
        this.fileRepo = fileRepo;
        this.versionRepo = versionRepo;
        this.sharedProjectFileRepo = sharedProjectFileRepo;
        this.currentUser = currentUser;
        this.blobStorageService = blobStorageService;
    }

    public async Task<List<ProjectFileWeb>> Handle(GetPackageFilesQuery request, CancellationToken cancellationToken)
    {
        // Verify package exists
        var packages = await packageRepo.GetBySearch(
            pfp => pfp.Id == request.PackageId &&
                   pfp.TenantId == request.TenantId &&
                   pfp.ProjectId == request.ProjectId &&
                   !pfp.IsDeleted
        );

        var package = packages.FirstOrDefault();
        if (package == null)
        {
            throw new NotFoundApiException(nameof(ProjectFilePackage), request.PackageId.ToString());
        }

        // Get files directly based on scope
        var filesList = await GetFilesForScopeAsync(request.PackageId, request.TenantId, request.ProjectId, request.Scope);
        
        if (filesList.Count == 0)
        {
            return new List<ProjectFileWeb>();
        }

        // Sort once before processing
        filesList.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));

        var fileIds = filesList.Select(f => f.Id).ToHashSet();
        var currentVersionIds = filesList
            .Where(f => f.CurrentVersionId.HasValue)
            .Select(f => f.CurrentVersionId!.Value)
            .ToList();

        // Sequential data fetching to avoid DbContext concurrency issues
        var currentVersionsDict = currentVersionIds.Count > 0
            ? (await versionRepo.GetBySearch(
                v => currentVersionIds.Contains(v.Id) && !v.IsDeleted,
                include => include.Include(v => v.CreatedByUser))).ToDictionary(v => v.Id)
            : new Dictionary<Guid, ProjectFileVersion>();

        var versionCountDict = await versionRepo.CountGroupedByAsync(
            v => fileIds.Contains(v.ProjectFileId) && !v.IsDeleted,
            v => v.ProjectFileId,
            cancellationToken);

        var sharedWithDict = request.Scope != ResourceScope.Mine
            ? (await sharedProjectFileRepo.GetBySearch(spf => fileIds.Contains(spf.ProjectFileId)))
                .GroupBy(spf => spf.ProjectFileId)
                .ToDictionary(g => g.Key, g => g.Select(spf => spf.SharedWithUserId).ToList())
            : new Dictionary<Guid, List<Guid>>();

        bool isOwnerView = request.Scope == ResourceScope.Mine;
        string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.Documentation);
        var emptyGuidList = new List<Guid>();

        var result = new List<ProjectFileWeb>(filesList.Count);
        foreach (var pf in filesList)
        {
            var currentVersion = pf.CurrentVersionId.HasValue 
                ? currentVersionsDict.GetValueOrDefault(pf.CurrentVersionId.Value) 
                : null;
            
            var sharedWith = sharedWithDict.TryGetValue(pf.Id, out var shared) 
                ? shared 
                : emptyGuidList;
            
            var totalVersions = versionCountDict.GetValueOrDefault(pf.Id, 0);

            result.Add(MapToProjectFileWeb(
                pf,
                package.Name,
                containerName,
                currentVersion,
                sharedWith,
                totalVersions,
                isOwnerView));
        }

        return result;
    }

    private async Task<List<ProjectFile>> GetFilesForScopeAsync(
        Guid packageId, 
        Guid tenantId, 
        Guid projectId, 
        ResourceScope scope)
    {
        return scope switch
        {
            ResourceScope.Mine => await GetMyFilesAsync(packageId, tenantId, projectId),
            ResourceScope.Shared => await GetSharedFilesAsync(packageId, tenantId, projectId),
            ResourceScope.All => await GetAllFilesAsync(packageId, tenantId, projectId),
            _ => new List<ProjectFile>()
        };
    }

    private async Task<List<ProjectFile>> GetMyFilesAsync(Guid packageId, Guid tenantId, Guid projectId)
    {
        var files = await fileRepo.GetBySearch(
            pf => pf.ProjectFilePackageId == packageId &&
                  pf.TenantId == tenantId &&
                  pf.ProjectId == projectId &&
                  pf.OwnerId == currentUser.Id &&
                  !pf.IsDeleted,
            include => include.Include(pf => pf.Owner)
        );
        return files.ToList();
    }

    private async Task<List<ProjectFile>> GetSharedFilesAsync(Guid packageId, Guid tenantId, Guid projectId)
    {
        // Get file IDs from shared files
        var fileIds = await sharedProjectFileRepo.SelectToHashSetAsync(
            spf => spf.TenantId == tenantId &&
                   spf.ProjectId == projectId &&
                   spf.SharedWithUserId == currentUser.Id &&
                   spf.ProjectFile.ProjectFilePackageId == packageId &&
                   !spf.ProjectFile.IsDeleted,
            spf => spf.ProjectFileId
        );

        if (fileIds.Count == 0)
        {
            return new List<ProjectFile>();
        }

        // Get files
        var files = await fileRepo.GetBySearch(
            pf => fileIds.Contains(pf.Id) && !pf.IsDeleted,
            include => include.Include(pf => pf.Owner)
        );
        return files.ToList();
    }

    private async Task<List<ProjectFile>> GetAllFilesAsync(Guid packageId, Guid tenantId, Guid projectId)
    {
        var files = await fileRepo.GetBySearch(
            pf => pf.ProjectFilePackageId == packageId &&
                  pf.TenantId == tenantId &&
                  pf.ProjectId == projectId &&
                  !pf.IsDeleted,
            include => include.Include(pf => pf.Owner)
        );
        return files.ToList();
    }

    private ProjectFileWeb MapToProjectFileWeb(
        ProjectFile pf,
        string packageName,
        string containerName,
        ProjectFileVersion? currentVersion,
        List<Guid> sharedWithUserIds,
        int totalVersions,
        bool isOwnerView)
    {
        ProjectFileVersionWeb? currentVersionWeb = null;

        if (currentVersion?.CreatedByUser != null)
        {
            string extension = Path.GetExtension(pf.FileName);
            string displayNameWithExtension = $"{pf.DisplayName}{extension}";

            Uri sasUriView = blobStorageService.GenerateSasUri(
                containerName, 
                currentVersion.BlobPath, 
                displayNameWithExtension, 
                expiresInMinutes: 60, 
                contentDisposition: "inline");
            
            Uri sasUriDownload = blobStorageService.GenerateSasUri(
                containerName, 
                currentVersion.BlobPath, 
                displayNameWithExtension, 
                expiresInMinutes: 60, 
                contentDisposition: "attachment");

            currentVersionWeb = new ProjectFileVersionWeb
            {
                Id = currentVersion.Id,
                ProjectFileId = currentVersion.ProjectFileId,
                VersionNumber = currentVersion.VersionNumber,
                ContentType = currentVersion.ContentType,
                FileSizeBytes = currentVersion.FileSizeBytes,
                CreatedAt = currentVersion.CreatedAt,
                CreatedByUserId = currentVersion.CreatedByUserId,
                CreatedByUserName = $"{currentVersion.CreatedByUser.FirstName} {currentVersion.CreatedByUser.LastName}".Trim(),
                SasUrlView = sasUriView.ToString(),
                SasUrlDownload = sasUriDownload.ToString(),
                Comments = new List<ProjectFileVersionCommentWeb>()
            };
        }

        return new ProjectFileWeb
        {
            Id = pf.Id,
            FileName = pf.FileName,
            DisplayName = pf.DisplayName,
            PackageName = packageName,
            CreatedAt = pf.CreatedAt,
            OwnerId = pf.OwnerId,
            OwnerName = $"{pf.Owner.FirstName} {pf.Owner.LastName}".Trim(),
            CurrentVersion = currentVersionWeb,
            Versions = new List<ProjectFileVersionWeb>(),
            TotalVersions = totalVersions,
            IsOwner = isOwnerView && pf.OwnerId == currentUser.Id,
            IsShared = sharedWithUserIds.Contains(currentUser.Id),
            SharedWithUserIds = sharedWithUserIds
        };
    }
}
