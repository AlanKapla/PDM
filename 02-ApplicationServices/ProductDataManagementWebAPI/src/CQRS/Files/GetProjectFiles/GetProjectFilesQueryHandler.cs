using Business.Interfaces.Configurations;
using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Files;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.GetProjectFiles;

public class GetProjectFilesQueryHandler : IRequestHandler<GetProjectFilesQuery, List<ProjectFilePackageWeb>>
{
    private readonly IRepository<ProjectFilePackage> packageRepo;
    private readonly IRepository<SharedProjectFile> sharedProjectFileRepo;
    private readonly ICurrentUser currentUser;
    private readonly IBlobStorageService blobStorageService;

    public GetProjectFilesQueryHandler(
        IRepository<ProjectFilePackage> packageRepo,
        IRepository<SharedProjectFile> sharedProjectFileRepo,
        ICurrentUser currentUser,
        IBlobStorageService blobStorageService)
    {
        this.packageRepo = packageRepo;
        this.sharedProjectFileRepo = sharedProjectFileRepo;
        this.currentUser = currentUser;
        this.blobStorageService = blobStorageService;
    }

    public async Task<List<ProjectFilePackageWeb>> Handle(GetProjectFilesQuery request, CancellationToken cancellationToken)
    {
        return request.Scope switch
        {
            ResourceScope.Mine => await GetMyFilesAsync(request.TenantId, request.ProjectId, cancellationToken),
            ResourceScope.Shared => await GetSharedFilesAsync(request.TenantId, request.ProjectId, cancellationToken),
            ResourceScope.All => await GetAllFilesAsync(request.TenantId, request.ProjectId, cancellationToken),
            _ => new List<ProjectFilePackageWeb>()
        };
    }

    private async Task<List<ProjectFilePackageWeb>> GetMyFilesAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken)
    {
        IEnumerable<ProjectFilePackage> packages = await packageRepo.GetBySearch(
            pfp => pfp.ProjectId == projectId &&
                   pfp.TenantId == tenantId &&
                   pfp.OwnerId == currentUser.Id &&
                   !pfp.IsDeleted,
            include => include.Include(pfp => pfp.Owner)
                              .Include(pfp => pfp.Files.Where(f => !f.IsDeleted))
                                 .ThenInclude(pf => pf.Owner)
                              .Include(pfp => pfp.Files.Where(f => !f.IsDeleted))
                                 .ThenInclude(pf => pf.SharedWith)
                              .Include(pfp => pfp.Files.Where(f => !f.IsDeleted))
                                 .ThenInclude(pf => pf.CurrentVersion)
                              .Include(pfp => pfp.Files.Where(f => !f.IsDeleted))
                                 .ThenInclude(pf => pf.Versions.Where(v => !v.IsDeleted))
                                 .ThenInclude(v => v.CreatedByUser)
                              .Include(pfp => pfp.Files.Where(f => !f.IsDeleted))
                                 .ThenInclude(pf => pf.Versions.Where(v => !v.IsDeleted))
                                 .ThenInclude(v => v.Comments.Where(c => !c.IsDeleted))
                                 .ThenInclude(c => c.User)
        );

        return MapToProjectFilePackageWeb(packages, isOwnerView: true);
    }

    private async Task<List<ProjectFilePackageWeb>> GetSharedFilesAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken)
    {
        IEnumerable<SharedProjectFile> sharedFiles = await sharedProjectFileRepo.GetBySearch(
            spf => spf.ProjectId == projectId &&
                   spf.TenantId == tenantId &&
                   spf.SharedWithUserId == currentUser.Id,
            include => include
                .Include(spf => spf.ProjectFile)
                    .ThenInclude(pf => pf.Package)
                    .ThenInclude(pkg => pkg.Owner)
                .Include(spf => spf.ProjectFile)
                    .ThenInclude(pf => pf.Owner)
                .Include(spf => spf.ProjectFile)
                    .ThenInclude(pf => pf.SharedWith)
                .Include(spf => spf.ProjectFile)
                    .ThenInclude(pf => pf.CurrentVersion)
                .Include(spf => spf.ProjectFile)
                    .ThenInclude(pf => pf.Versions.Where(v => !v.IsDeleted))
                    .ThenInclude(v => v.CreatedByUser)
                .Include(spf => spf.ProjectFile)
                    .ThenInclude(pf => pf.Versions.Where(v => !v.IsDeleted))
                    .ThenInclude(v => v.Comments.Where(c => !c.IsDeleted))
                    .ThenInclude(c => c.User)
                .Include(spf => spf.SharedByUser)
        );

        // Group shared files by package
        var packageGroups = sharedFiles
            .GroupBy(spf => spf.ProjectFile.Package)
            .Select(group => new 
            {
                Package = group.Key,
                Files = group.Select(spf => spf.ProjectFile).ToList()
            });

        var result = new List<ProjectFilePackageWeb>();

        foreach (var group in packageGroups)
        {
            var filesWeb = group.Files
                .Where(pf => !pf.IsDeleted)
                .Select(pf => MapToProjectFileWeb(pf, isOwnerView: false))
                .OrderByDescending(pf => pf.CreatedAt)
                .ToList();

            result.Add(new ProjectFilePackageWeb
            {
                Id = group.Package.Id,
                Name = group.Package.Name,
                CreatedAt = group.Package.CreatedAt,
                OwnerId = group.Package.OwnerId,
                OwnerName = $"{group.Package.Owner.FirstName} {group.Package.Owner.LastName}".Trim(),
                Files = filesWeb,
                TotalFiles = filesWeb.Count
            });
        }

        return result.OrderByDescending(p => p.CreatedAt).ToList();
    }

    private async Task<List<ProjectFilePackageWeb>> GetAllFilesAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken)
    {
        IEnumerable<ProjectFilePackage> packages = await packageRepo.GetBySearch(
            pfp => pfp.ProjectId == projectId &&
                   pfp.TenantId == tenantId &&
                   !pfp.IsDeleted,
            include => include.Include(pfp => pfp.Owner)
                              .Include(pfp => pfp.Files.Where(f => !f.IsDeleted))
                                 .ThenInclude(pf => pf.Owner)
                              .Include(pfp => pfp.Files.Where(f => !f.IsDeleted))
                                 .ThenInclude(pf => pf.SharedWith)
                              .Include(pfp => pfp.Files.Where(f => !f.IsDeleted))
                                 .ThenInclude(pf => pf.CurrentVersion)
                              .Include(pfp => pfp.Files.Where(f => !f.IsDeleted))
                                 .ThenInclude(pf => pf.Versions.Where(v => !v.IsDeleted))
                                 .ThenInclude(v => v.CreatedByUser)
                              .Include(pfp => pfp.Files.Where(f => !f.IsDeleted))
                                 .ThenInclude(pf => pf.Versions.Where(v => !v.IsDeleted))
                                 .ThenInclude(v => v.Comments.Where(c => !c.IsDeleted))
                                 .ThenInclude(c => c.User)
        );

        return MapToProjectFilePackageWeb(packages, isOwnerView: false);
    }

    private List<ProjectFilePackageWeb> MapToProjectFilePackageWeb(IEnumerable<ProjectFilePackage> packages, bool isOwnerView)
    {
        string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.Documentation);

        return packages.Select(package =>
        {
            var filesWeb = package.Files
                .Where(pf => !pf.IsDeleted)
                .Select(pf => MapToProjectFileWeb(pf, isOwnerView))
                .OrderByDescending(pf => pf.CreatedAt)
                .ToList();

            return new ProjectFilePackageWeb
            {
                Id = package.Id,
                Name = package.Name,
                CreatedAt = package.CreatedAt,
                OwnerId = package.OwnerId,
                OwnerName = $"{package.Owner.FirstName} {package.Owner.LastName}".Trim(),
                Files = filesWeb,
                TotalFiles = filesWeb.Count
            };
        })
        .OrderByDescending(p => p.CreatedAt)
        .ToList();
    }

    private ProjectFileWeb MapToProjectFileWeb(ProjectFile pf, bool isOwnerView)
    {
        string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.Documentation);
        
        ProjectFileVersionWeb? currentVersionWeb = null;
        
        if (pf.CurrentVersion != null)
        {
            string extension = Path.GetExtension(pf.FileName);
            string displayNameWithExtension = $"{pf.DisplayName}{extension}";
            
            Uri sasUriView = blobStorageService.GenerateSasUri(containerName, pf.CurrentVersion.BlobPath, displayNameWithExtension, expiresInMinutes: 60, contentDisposition: "inline");
            Uri sasUriDownload = blobStorageService.GenerateSasUri(containerName, pf.CurrentVersion.BlobPath, displayNameWithExtension, expiresInMinutes: 60, contentDisposition: "attachment");
            
            var currentVersionComments = pf.Versions
                .Where(v => v.Id == pf.CurrentVersion.Id && !v.IsDeleted)
                .SelectMany(v => v.Comments.Where(c => !c.IsDeleted))
                .Select(c => new ProjectFileVersionCommentWeb
                {
                    Id = c.Id,
                    ProjectFileVersionId = c.ProjectFileVersionId,
                    UserId = c.UserId,
                    UserName = $"{c.User.FirstName} {c.User.LastName}".Trim(),
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    EditedAt = c.EditedAt,
                    IsEdited = c.EditedAt.HasValue,
                    CanEdit = c.UserId == currentUser.Id,
                    CanDelete = c.UserId == currentUser.Id
                })
                .OrderBy(c => c.CreatedAt)
                .ToList();
            
            currentVersionWeb = new ProjectFileVersionWeb
            {
                Id = pf.CurrentVersion.Id,
                ProjectFileId = pf.CurrentVersion.ProjectFileId,
                VersionNumber = pf.CurrentVersion.VersionNumber,
                ContentType = pf.CurrentVersion.ContentType,
                FileSizeBytes = pf.CurrentVersion.FileSizeBytes,
                CreatedAt = pf.CurrentVersion.CreatedAt,
                CreatedByUserId = pf.CurrentVersion.CreatedByUserId,
                CreatedByUserName = $"{pf.Owner.FirstName} {pf.Owner.LastName}".Trim(),
                SasUrlView = sasUriView.ToString(),
                SasUrlDownload = sasUriDownload.ToString(),
                Comments = currentVersionComments
            };
        }
        
        var allVersionsWeb = pf.Versions
            .Where(v => !v.IsDeleted)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v =>
            {
                string extension = Path.GetExtension(pf.FileName);
                
                bool isCurrentVersion = pf.CurrentVersionId.HasValue && v.Id == pf.CurrentVersionId.Value;
                string fileNameWithVersion = isCurrentVersion
                    ? $"{pf.DisplayName}{extension}"
                    : $"{pf.DisplayName}_v{v.VersionNumber}{extension}";

                Uri sasUriView = blobStorageService.GenerateSasUri(containerName, v.BlobPath, fileNameWithVersion, expiresInMinutes: 60, contentDisposition: "inline");
                Uri sasUriDownload = blobStorageService.GenerateSasUri(containerName, v.BlobPath, fileNameWithVersion, expiresInMinutes: 60, contentDisposition: "attachment");
                
                var versionComments = v.Comments
                    .Where(c => !c.IsDeleted)
                    .Select(c => new ProjectFileVersionCommentWeb
                    {
                        Id = c.Id,
                        ProjectFileVersionId = c.ProjectFileVersionId,
                        UserId = c.UserId,
                        UserName = $"{c.User.FirstName} {c.User.LastName}".Trim(),
                        Content = c.Content,
                        CreatedAt = c.CreatedAt,
                        EditedAt = c.EditedAt,
                        IsEdited = c.EditedAt.HasValue,
                        CanEdit = c.UserId == currentUser.Id,
                        CanDelete = c.UserId == currentUser.Id
                    })
                    .OrderBy(c => c.CreatedAt)
                    .ToList();
                
                return new ProjectFileVersionWeb
                {
                    Id = v.Id,
                    ProjectFileId = v.ProjectFileId,
                    VersionNumber = v.VersionNumber,
                    ContentType = v.ContentType,
                    FileSizeBytes = v.FileSizeBytes,
                    CreatedAt = v.CreatedAt,
                    CreatedByUserId = v.CreatedByUserId,
                    CreatedByUserName = $"{v.CreatedByUser.FirstName} {v.CreatedByUser.LastName}".Trim(),
                    SasUrlView = sasUriView.ToString(),
                    SasUrlDownload = sasUriDownload.ToString(),
                    Comments = versionComments
                };
            })
            .ToList();
        
        return new ProjectFileWeb
        {
            Id = pf.Id,
            FileName = pf.FileName,
            DisplayName = pf.DisplayName,
            PackageName = pf.Package.Name,
            CreatedAt = pf.CreatedAt,
            OwnerId = pf.OwnerId,
            OwnerName = $"{pf.Owner.FirstName} {pf.Owner.LastName}".Trim(),
            CurrentVersion = currentVersionWeb,
            Versions = allVersionsWeb,
            TotalVersions = pf.Versions.Count(v => !v.IsDeleted),
            IsOwner = isOwnerView && pf.OwnerId == currentUser.Id,
            IsShared = pf.SharedWith.Any(sw => sw.SharedWithUserId == currentUser.Id),
            SharedWithUserIds = pf.SharedWith.Select(sw => sw.SharedWithUserId).ToList()
        };
    }
}
