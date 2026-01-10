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

namespace CQRS.Files.GetFileVersions;

public class GetFileVersionsQueryHandler : IRequestHandler<GetFileVersionsQuery, List<ProjectFileVersionWeb>>
{
    private readonly IRepository<ProjectFile> fileRepo;
    private readonly IRepository<ProjectFileVersion> versionRepo;
    private readonly IRepository<SharedProjectFile> sharedProjectFileRepo;
    private readonly ICurrentUser currentUser;
    private readonly IBlobStorageService blobStorageService;

    public GetFileVersionsQueryHandler(
        IRepository<ProjectFile> fileRepo,
        IRepository<ProjectFileVersion> versionRepo,
        IRepository<SharedProjectFile> sharedProjectFileRepo,
        ICurrentUser currentUser,
        IBlobStorageService blobStorageService)
    {
        this.fileRepo = fileRepo;
        this.versionRepo = versionRepo;
        this.sharedProjectFileRepo = sharedProjectFileRepo;
        this.currentUser = currentUser;
        this.blobStorageService = blobStorageService;
    }

    public async Task<List<ProjectFileVersionWeb>> Handle(GetFileVersionsQuery request, CancellationToken cancellationToken)
    {
        // Get file
        var files = await fileRepo.GetBySearch(
            pf => pf.Id == request.FileId &&
                  pf.TenantId == request.TenantId &&
                  pf.ProjectId == request.ProjectId &&
                  !pf.IsDeleted
        );

        var file = files.FirstOrDefault();
        if (file == null)
        {
            throw new NotFoundApiException(nameof(ProjectFile), request.FileId.ToString());
        }

        // Check access
        if (!await HasAccessToFileAsync(file, request.Scope, request.TenantId, request.ProjectId))
        {
            return new List<ProjectFileVersionWeb>();
        }

        // Get versions with creator
        var versions = await versionRepo.GetBySearch(
            v => v.ProjectFileId == request.FileId && !v.IsDeleted,
            include => include.Include(v => v.CreatedByUser)
        );

        string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.Documentation);
        string extension = Path.GetExtension(file.FileName);

        return versions
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => MapToVersionWeb(v, file, extension, containerName))
            .ToList();
    }

    private async Task<bool> HasAccessToFileAsync(
        ProjectFile file,
        ResourceScope scope,
        Guid tenantId,
        Guid projectId)
    {
        return scope switch
        {
            ResourceScope.Mine => file.OwnerId == currentUser.Id,
            ResourceScope.Shared => await IsFileSharedWithUserAsync(file.Id, tenantId, projectId),
            ResourceScope.All => true,
            _ => false
        };
    }

    private async Task<bool> IsFileSharedWithUserAsync(Guid fileId, Guid tenantId, Guid projectId)
    {
        var sharedFiles = await sharedProjectFileRepo.GetBySearch(
            spf => spf.TenantId == tenantId &&
                   spf.ProjectId == projectId &&
                   spf.ProjectFileId == fileId &&
                   spf.SharedWithUserId == currentUser.Id
        );
        return sharedFiles.Any();
    }

    private ProjectFileVersionWeb MapToVersionWeb(
        ProjectFileVersion version,
        ProjectFile file,
        string extension,
        string containerName)
    {
        bool isCurrentVersion = file.CurrentVersionId.HasValue && version.Id == file.CurrentVersionId.Value;
        string fileNameWithVersion = isCurrentVersion
            ? $"{file.DisplayName}{extension}"
            : $"{file.DisplayName}_v{version.VersionNumber}{extension}";

        Uri sasUriView = blobStorageService.GenerateSasUri(
            containerName,
            version.BlobPath,
            fileNameWithVersion,
            expiresInMinutes: 60,
            contentDisposition: "inline");

        Uri sasUriDownload = blobStorageService.GenerateSasUri(
            containerName,
            version.BlobPath,
            fileNameWithVersion,
            expiresInMinutes: 60,
            contentDisposition: "attachment");

        return new ProjectFileVersionWeb
        {
            Id = version.Id,
            ProjectFileId = version.ProjectFileId,
            VersionNumber = version.VersionNumber,
            ContentType = version.ContentType,
            FileSizeBytes = version.FileSizeBytes,
            CreatedAt = version.CreatedAt,
            CreatedByUserId = version.CreatedByUserId,
            CreatedByUserName = $"{version.CreatedByUser.FirstName} {version.CreatedByUser.LastName}".Trim(),
            SasUrlView = sasUriView.ToString(),
            SasUrlDownload = sasUriDownload.ToString(),
            Comments = new List<ProjectFileVersionCommentWeb>()
        };
    }
}
