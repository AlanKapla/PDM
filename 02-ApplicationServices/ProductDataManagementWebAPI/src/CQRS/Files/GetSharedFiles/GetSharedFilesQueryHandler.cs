using Business.Interfaces.Configurations;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Files;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.GetSharedFiles
{
    public class GetSharedFilesQueryHandler : IRequestHandler<GetSharedFilesQuery, List<SharedProjectFilePackageWeb>>
    {
        private readonly IRepository<SharedProjectFile> sharedProjectFileRepo;
        private readonly ICurrentUser currentUser;
        private readonly IBlobStorageService blobStorageService;

        public GetSharedFilesQueryHandler(
            IRepository<SharedProjectFile> sharedProjectFileRepo,
            ICurrentUser currentUser,
            IBlobStorageService blobStorageService)
        {
            this.sharedProjectFileRepo = sharedProjectFileRepo;
            this.currentUser = currentUser;
            this.blobStorageService = blobStorageService;
        }

        public async Task<List<SharedProjectFilePackageWeb>> Handle(GetSharedFilesQuery request, CancellationToken cancellationToken)
        {
            IEnumerable<SharedProjectFile> sharedFiles = await sharedProjectFileRepo.GetBySearch(
                spf => spf.ProjectId == request.ProjectId &&
                       spf.TenantId == request.TenantId &&
                       spf.SharedWithUserId == currentUser.Id,
                include => include
                    .Include(spf => spf.ProjectFile)
                        .ThenInclude(pf => pf.Package)
                        .ThenInclude(pkg => pkg.Owner)
                    .Include(spf => spf.ProjectFile)
                        .ThenInclude(pf => pf.Owner)
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

            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.Documentation);

            // Group shared files by package
            var groupedByPackage = sharedFiles
                .GroupBy(spf => new 
                { 
                    PackageId = spf.ProjectFile.ProjectFilePackageId,
                    PackageName = spf.ProjectFile.Package.Name,
                    PackageOwnerId = spf.ProjectFile.Package.OwnerId,
                    PackageOwnerName = $"{spf.ProjectFile.Package.Owner.FirstName} {spf.ProjectFile.Package.Owner.LastName}".Trim()
                })
                .Select(packageGroup => new SharedProjectFilePackageWeb
                {
                    PackageId = packageGroup.Key.PackageId,
                    PackageName = packageGroup.Key.PackageName,
                    PackageOwnerId = packageGroup.Key.PackageOwnerId,
                    PackageOwnerName = packageGroup.Key.PackageOwnerName,
                    TotalSharedFiles = packageGroup.Count(),
                    Files = packageGroup.Select(spf =>
                    {
                        string sasUrl = string.Empty;
                        long fileSizeBytes = 0;
                        string contentType = string.Empty;
                        DateTime uploadedAt = spf.ProjectFile.CreatedAt;
                        ProjectFileVersionWeb? currentVersionWeb = null;

                        if (spf.ProjectFile.CurrentVersion != null)
                        {
                            string extension = Path.GetExtension(spf.ProjectFile.FileName);
                            string displayNameWithExtension = $"{spf.ProjectFile.DisplayName}{extension}";
                            
                            Uri sasUriView = blobStorageService.GenerateSasUri(
                                containerName, 
                                spf.ProjectFile.CurrentVersion.BlobPath, 
                                displayNameWithExtension, 
                                expiresInMinutes: 60,
                                contentDisposition: "inline");
                            
                            Uri sasUriDownload = blobStorageService.GenerateSasUri(
                                containerName, 
                                spf.ProjectFile.CurrentVersion.BlobPath, 
                                displayNameWithExtension, 
                                expiresInMinutes: 60,
                                contentDisposition: "attachment");
                            
                            sasUrl = sasUriView.ToString();
                            fileSizeBytes = spf.ProjectFile.CurrentVersion.FileSizeBytes;
                            contentType = spf.ProjectFile.CurrentVersion.ContentType;
                            uploadedAt = spf.ProjectFile.CurrentVersion.CreatedAt;
                            
                            var currentVersionComments = spf.ProjectFile.Versions
                                .Where(v => v.Id == spf.ProjectFile.CurrentVersion.Id && !v.IsDeleted)
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
                                Id = spf.ProjectFile.CurrentVersion.Id,
                                ProjectFileId = spf.ProjectFile.CurrentVersion.ProjectFileId,
                                VersionNumber = spf.ProjectFile.CurrentVersion.VersionNumber,
                                ContentType = spf.ProjectFile.CurrentVersion.ContentType,
                                FileSizeBytes = spf.ProjectFile.CurrentVersion.FileSizeBytes,
                                CreatedAt = spf.ProjectFile.CurrentVersion.CreatedAt,
                                CreatedByUserId = spf.ProjectFile.CurrentVersion.CreatedByUserId,
                                CreatedByUserName = $"{spf.ProjectFile.Owner.FirstName} {spf.ProjectFile.Owner.LastName}".Trim(),
                                SasUrlView = sasUriView.ToString(),
                                SasUrlDownload = sasUriDownload.ToString(),
                                Comments = currentVersionComments
                            };
                        }
                        
                        var allVersionsWeb = spf.ProjectFile.Versions
                            .Where(v => !v.IsDeleted)
                            .OrderByDescending(v => v.VersionNumber)
                            .Select(v =>
                            {
                                string extension = Path.GetExtension(spf.ProjectFile.FileName);
                                
                                bool isCurrentVersion = spf.ProjectFile.CurrentVersionId.HasValue && v.Id == spf.ProjectFile.CurrentVersionId.Value;
                                string fileNameWithVersion = isCurrentVersion 
                                    ? $"{spf.ProjectFile.DisplayName}{extension}"
                                    : $"{spf.ProjectFile.DisplayName}_v{v.VersionNumber}{extension}";

                                Uri sasUriView = blobStorageService.GenerateSasUri(
                                    containerName, 
                                    v.BlobPath, 
                                    fileNameWithVersion, 
                                    expiresInMinutes: 60,
                                    contentDisposition: "inline");
                                
                                Uri sasUriDownload = blobStorageService.GenerateSasUri(
                                    containerName, 
                                    v.BlobPath, 
                                    fileNameWithVersion, 
                                    expiresInMinutes: 60,
                                    contentDisposition: "attachment");
                                
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

                        return new SharedProjectFileWeb
                        {
                            Id = spf.Id,
                            ProjectFileId = spf.ProjectFileId,
                            FileName = spf.ProjectFile.FileName,
                            DisplayName = spf.ProjectFile.DisplayName,
                            PackageName = spf.ProjectFile.Package.Name,
                            ContentType = contentType,
                            FileSizeBytes = fileSizeBytes,
                            UploadedAt = uploadedAt,
                            SharedAt = spf.SharedAt,
                            SharedByUserId = spf.SharedByUserId,
                            SharedByUserName = $"{spf.SharedByUser.FirstName} {spf.SharedByUser.LastName}".Trim(),
                            OriginalOwnerUserId = spf.ProjectFile.OwnerId,
                            OriginalOwnerUserName = $"{spf.ProjectFile.Owner.FirstName} {spf.ProjectFile.Owner.LastName}".Trim(),
                            SasUrl = sasUrl,
                            CurrentVersion = currentVersionWeb,
                            Versions = allVersionsWeb,
                            TotalVersions = spf.ProjectFile.Versions.Count(v => !v.IsDeleted)
                        };
                    })
                    .OrderByDescending(f => f.SharedAt)
                    .ToList()
                })
                .OrderBy(pkg => pkg.PackageOwnerName)
                .ThenBy(pkg => pkg.PackageName)
                .ToList();

            return groupedByPackage;
        }
    }
}
