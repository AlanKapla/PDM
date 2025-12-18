using Business.Interfaces.Configurations;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Files;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.GetUserUploadedFiles
{
    public class GetUserUploadedFilesQueryHandler : IRequestHandler<GetUserUploadedFilesQuery, List<ProjectFilePackageWeb>>
    {
        private readonly IRepository<ProjectFilePackage> packageRepo;
        private readonly ICurrentUser currentUser;
        private readonly IBlobStorageService blobStorageService;

        public GetUserUploadedFilesQueryHandler(
            IRepository<ProjectFilePackage> packageRepo,
            ICurrentUser currentUser,
            IBlobStorageService blobStorageService)
        {
            this.packageRepo = packageRepo;
            this.currentUser = currentUser;
            this.blobStorageService = blobStorageService;
        }

        public async Task<List<ProjectFilePackageWeb>> Handle(GetUserUploadedFilesQuery request, CancellationToken cancellationToken)
        {
            // Query packages first, then load files through them
            IEnumerable<ProjectFilePackage> packages = await packageRepo.GetBySearch(
                pfp => pfp.ProjectId == request.ProjectId &&
                       pfp.TenantId == request.TenantId &&
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

            string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.Documentation);

            var result = packages.Select(package =>
            {
                var filesWeb = package.Files
                    .Where(pf => !pf.IsDeleted)
                    .Select(pf =>
                    {
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
                            PackageName = package.Name,
                            CreatedAt = pf.CreatedAt,
                            OwnerId = pf.OwnerId,
                            OwnerName = $"{pf.Owner.FirstName} {pf.Owner.LastName}".Trim(),
                            CurrentVersion = currentVersionWeb,
                            Versions = allVersionsWeb,
                            TotalVersions = pf.Versions.Count(v => !v.IsDeleted),
                            IsOwner = true,
                            IsShared = false,
                            SharedWithUserIds = pf.SharedWith.Select(sw => sw.SharedWithUserId).ToList()
                        };
                    })
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

            return result;
        }
    }
}
