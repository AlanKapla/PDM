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
    private readonly IFileAccessService fileAccessService;
    private readonly ICurrentUser currentUser;
    private readonly IBlobStorageService blobStorageService;

    public GetPackageFilesQueryHandler(
        IRepository<ProjectFilePackage> packageRepo,
        IRepository<ProjectFile> fileRepo,
        IRepository<ProjectFileVersion> versionRepo,
        IRepository<SharedProjectFile> sharedProjectFileRepo,
        IFileAccessService fileAccessService,
        ICurrentUser currentUser,
        IBlobStorageService blobStorageService)
    {
        this.packageRepo = packageRepo;
        this.fileRepo = fileRepo;
        this.versionRepo = versionRepo;
        this.sharedProjectFileRepo = sharedProjectFileRepo;
        this.fileAccessService = fileAccessService;
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

        // Dla Mine i All - pobierz informacje o udostępnieniach (do edycji)
        // Dla Shared - nie potrzebujemy (tylko przeglądamy)
        Dictionary<Guid, List<Guid>> sharedWithDict = new();
        
        if (request.Scope == ResourceScope.Mine || request.Scope == ResourceScope.All)
        {
            sharedWithDict = await BuildSharedWithDictionaryAsync(request.PackageId, fileIds, cancellationToken);
        }

        bool isOwnerView = request.Scope == ResourceScope.Mine;
        string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.Documentation);

        var result = new List<ProjectFileWeb>(filesList.Count);
        foreach (var pf in filesList)
        {
            var currentVersion = pf.CurrentVersionId.HasValue 
                ? currentVersionsDict.GetValueOrDefault(pf.CurrentVersionId.Value) 
                : null;
            
            var totalVersions = versionCountDict.GetValueOrDefault(pf.Id, 0);
            
            var sharedWithUserIds = sharedWithDict.TryGetValue(pf.Id, out var shared) 
                ? shared 
                : new List<Guid>();

            result.Add(MapToProjectFileWeb(
                pf,
                package.Name,
                containerName,
                currentVersion,
                totalVersions,
                isOwnerView,
                sharedWithUserIds));
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
        // ✅ Pobierz informacje o dostępie
        var accessInfo = await fileAccessService.GetPackageAccessInfoAsync(
            currentUser.Id,
            packageId);

        if (accessInfo.IsPackageShared)
        {
            // Paczka udostępniona - pobierz wszystkie pliki OPRÓCZ wykluczeń
            var files = await fileRepo.GetBySearch(
                pf => pf.ProjectFilePackageId == packageId &&
                      pf.TenantId == tenantId &&
                      pf.ProjectId == projectId &&
                      !accessInfo.ExcludedFileIds.Contains(pf.Id) &&
                      !pf.IsDeleted,
                include => include.Include(pf => pf.Owner)
            );
            return files.ToList();
        }
        else
        {
            // Paczka NIE udostępniona - pobierz tylko pliki z Allow
            if (!accessInfo.AllowedFileIds.Any())
            {
                return new List<ProjectFile>();
            }

            var files = await fileRepo.GetBySearch(
                pf => accessInfo.AllowedFileIds.Contains(pf.Id) && !pf.IsDeleted,
                include => include.Include(pf => pf.Owner)
            );
            return files.ToList();
        }
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
        int totalVersions,
        bool isOwnerView,
        List<Guid> sharedWithUserIds)
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
            IsShared = sharedWithUserIds.Any(),  // ✅ Ma udostępnienia
            SharedWithUserIds = sharedWithUserIds  // ✅ Lista userów (puste dla Shared scope)
        };
    }

    /// <summary>
    /// Buduje słownik: FileId -> Lista UserIds którzy mają dostęp
    /// Uwzględnia Package + Allow/Deny model:
    /// - User ma dostęp jeśli: (Package shared AND NIE ma Deny) OR (ma Allow)
    /// </summary>
    private async Task<Dictionary<Guid, List<Guid>>> BuildSharedWithDictionaryAsync(
        Guid packageId,
        HashSet<Guid> fileIds,
        CancellationToken cancellationToken)
    {
        // Pobierz wszystkie udostępnienia dla paczki
        var allShares = await sharedProjectFileRepo.GetBySearch(
            spf => spf.ProjectFilePackageId == packageId);

        // Grupuj po userId
        var sharesByUser = allShares.GroupBy(s => s.SharedWithUserId);

        var result = new Dictionary<Guid, List<Guid>>();

        foreach (var fileId in fileIds)
        {
            var usersWithAccess = new List<Guid>();

            foreach (var userShares in sharesByUser)
            {
                var userId = userShares.Key;
                
                // Sprawdź czy user ma dostęp do tego pliku
                var packageShare = userShares.FirstOrDefault(s => s.ProjectFileId == null);
                var fileShare = userShares.FirstOrDefault(s => s.ProjectFileId == fileId);

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
                result[fileId] = usersWithAccess;
            }
        }

        return result;
    }

}
