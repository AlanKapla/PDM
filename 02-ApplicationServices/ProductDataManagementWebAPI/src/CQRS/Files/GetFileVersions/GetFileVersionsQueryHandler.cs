using Business.Interfaces.Configurations;
using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Files;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.GetFileVersions;

public class GetFileVersionsQueryHandler : IRequestHandler<GetFileVersionsQuery, List<ProjectFileVersionWeb>>
{
    private readonly IProjectFilesService projectFilesService;
    private readonly IReadRepository<User> userRepository;
    private readonly ICurrentUser currentUser;
    private readonly IBlobStorageService blobStorageService;

    public GetFileVersionsQueryHandler(
        IProjectFilesService projectFilesService,
        IReadRepository<User> userRepository,
        ICurrentUser currentUser,
        IBlobStorageService blobStorageService)
    {
        this.projectFilesService = projectFilesService;
        this.userRepository = userRepository;
        this.currentUser = currentUser;
        this.blobStorageService = blobStorageService;
    }

    public async Task<List<ProjectFileVersionWeb>> Handle(GetFileVersionsQuery request, CancellationToken cancellationToken)
    {
        // Get file from cache
        Dictionary<Guid, List<ProjectFileCacheDto>> allFilesByPackage = await projectFilesService.GetProjectPackageFilesAsync(
            request.TenantId,
            request.ProjectId,
            cancellationToken);

        // Find file in all packages
        ProjectFileCacheDto? fileDto = allFilesByPackage.Values
            .SelectMany(files => files)
            .FirstOrDefault(f => f.Id == request.FileId);

        if (fileDto == null)
        {
            throw new NotFoundApiException(nameof(ProjectFile), request.FileId.ToString());
        }

        // Check access based on scope
        bool hasAccess = await HasAccessToFileAsync(
            fileDto,
            request.Scope,
            request.TenantId,
            request.ProjectId,
            cancellationToken);

        if (!hasAccess)
        {
            return new List<ProjectFileVersionWeb>();
        }

        // Get versions from cache
        Dictionary<Guid, List<ProjectFileVersionDto>> allVersionsByFile = await projectFilesService.GetProjectFilesVersionsAsync(
            request.TenantId,
            request.ProjectId,
            cancellationToken);

        if (!allVersionsByFile.TryGetValue(request.FileId, out List<ProjectFileVersionDto>? versionDtos))
        {
            return new List<ProjectFileVersionWeb>();
        }

        // Get unique CreatedByUserId and fetch users as dictionary
        HashSet<Guid> createdByUserIds = versionDtos.Select(v => v.CreatedByUserId).ToHashSet();

        Dictionary<Guid, User> userDict = await userRepository.GetDictionaryBySearchAsync(
            u => createdByUserIds.Contains(u.Id),
            cancellationToken);

        string containerName = BlobStorageSettings.GetContainerName(BlobContainerNames.Documentation);
        string extension = Path.GetExtension(fileDto.FileName);

        return versionDtos
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => MapToVersionWeb(v, fileDto, extension, containerName, userDict))
            .ToList();
    }

    private async Task<bool> HasAccessToFileAsync(
        ProjectFileCacheDto fileDto,
        ResourceScope scope,
        Guid tenantId,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        if (scope == ResourceScope.Mine)
        {
            return fileDto.OwnerId == currentUser.Id;
        }

        if (scope == ResourceScope.All)
        {
            return true;
        }

        // ResourceScope.Shared - use ProjectFilesService
        return await projectFilesService.HasAccessToFileAsync(
            currentUser,
            tenantId,
            projectId,
            fileDto.ProjectFilePackageId,
            fileDto.Id,
            ResourceScope.Shared,
            cancellationToken);
    }

    private ProjectFileVersionWeb MapToVersionWeb(
        ProjectFileVersionDto versionDto,
        ProjectFileCacheDto fileDto,
        string extension,
        string containerName,
        Dictionary<Guid, User> userDict)
    {
        bool isCurrentVersion = fileDto.CurrentVersionId.HasValue && versionDto.Id == fileDto.CurrentVersionId.Value;
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

        string createdByUserName = string.Empty;
        if (userDict.TryGetValue(versionDto.CreatedByUserId, out User? user))
        {
            createdByUserName = $"{user.FirstName} {user.LastName}".Trim();
        }

        return new ProjectFileVersionWeb
        {
            Id = versionDto.Id,
            ProjectFileId = versionDto.ProjectFileId,
            VersionNumber = versionDto.VersionNumber,
            ContentType = versionDto.ContentType,
            FileSizeBytes = versionDto.FileSizeBytes,
            CreatedAt = versionDto.CreatedAt,
            CreatedByUserId = versionDto.CreatedByUserId,
            CreatedByUserName = createdByUserName,
            SasUrlView = sasUriView.ToString(),
            SasUrlDownload = sasUriDownload.ToString(),
            Comments = new List<ProjectFileVersionCommentWeb>()
        };
    }
}
