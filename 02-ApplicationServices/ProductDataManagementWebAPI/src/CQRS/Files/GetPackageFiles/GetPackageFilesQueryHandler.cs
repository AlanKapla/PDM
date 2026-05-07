using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Files;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using MediatR;

namespace CQRS.Files.GetPackageFiles;

public class GetPackageFilesQueryHandler : IRequestHandler<GetPackageFilesQuery, List<ProjectFileWeb>>
{
    private readonly IProjectFilesService projectFilesService;
    private readonly IUserService userService;
    private readonly ICurrentUser currentUser;

    public GetPackageFilesQueryHandler(
        IProjectFilesService projectFilesService,
        IUserService userService,
        ICurrentUser currentUser)
    {
        this.projectFilesService = projectFilesService;
        this.userService = userService;
        this.currentUser = currentUser;
    }

    public async Task<List<ProjectFileWeb>> Handle(GetPackageFilesQuery request, CancellationToken cancellationToken)
    {
        ProjectFilePackageDto? packageDto = await projectFilesService.GetAccessiblePackageByIdAsync(
            currentUser, request.TenantId, request.ProjectId, request.PackageId, request.Scope, cancellationToken)
            ?? throw new NotFoundApiException(nameof(ProjectFilePackage), request.PackageId.ToString());

        List<ProjectFileCacheDto> accessibleFiles = await projectFilesService.GetAccessibleFilesAsync(
            currentUser, request.TenantId, request.ProjectId, request.PackageId, request.Scope, cancellationToken);

        if (accessibleFiles.Count == 0)
        {
            return new List<ProjectFileWeb>();
        }

        accessibleFiles.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));

        FileVersionsSummary versionsSummary = await projectFilesService.GetFileVersionsSummaryAsync(
            request.TenantId, request.ProjectId, accessibleFiles, cancellationToken);

        Dictionary<Guid, FileVersionSasUriInfo> sasUrisDict = versionsSummary.CurrentVersionIds.Count > 0
            ? await projectFilesService.GetFileVersionsSasUrisAsync(request.TenantId, request.ProjectId, versionsSummary.CurrentVersionIds.ToArray())
            : new Dictionary<Guid, FileVersionSasUriInfo>();

        HashSet<Guid> fileIds = accessibleFiles.Select(f => f.Id).ToHashSet();

        Dictionary<Guid, List<Guid>> sharedWithDict = (request.Scope == ResourceScope.Mine || request.Scope == ResourceScope.All)
            ? await projectFilesService.GetSharedWithUsersAsync(request.TenantId, request.ProjectId, request.PackageId, fileIds, cancellationToken)
            : new Dictionary<Guid, List<Guid>>();

        ProjectFileVersionsResult versionsResult = versionsSummary.CurrentVersionIds.Count > 0
            ? await projectFilesService.GetVersionsByIdsAsync(request.TenantId, request.ProjectId, versionsSummary.CurrentVersionIds, cancellationToken)
            : new ProjectFileVersionsResult();

        HashSet<Guid> allUserIds = [.. versionsResult.CreatedByUserIds];

        foreach (ProjectFileCacheDto file in accessibleFiles)
        {
            allUserIds.Add(file.OwnerId);
        }

        Dictionary<Guid, ProjectMemberUserInfo> userDict = await userService.GetProjectMembersByIdsAsync(
            request.TenantId, request.ProjectId, allUserIds, cancellationToken);

        bool isOwnerView = request.Scope == ResourceScope.Mine;

        List<ProjectFileWeb> result = new List<ProjectFileWeb>(accessibleFiles.Count);

        foreach (ProjectFileCacheDto fileDto in accessibleFiles)
        {
            ProjectFileVersionDto? currentVersionDto = fileDto.CurrentVersionId.HasValue
                ? versionsResult.Versions.GetValueOrDefault(fileDto.CurrentVersionId.Value)
                : null;

            int totalVersions = versionsSummary.VersionCounts.GetValueOrDefault(fileDto.Id, 0);

            List<Guid> sharedWithUserIds = sharedWithDict.TryGetValue(fileDto.Id, out List<Guid>? shared)
                ? shared
                : new List<Guid>();

            FileVersionSasUriInfo? sasUris = fileDto.CurrentVersionId.HasValue
                ? sasUrisDict.GetValueOrDefault(fileDto.CurrentVersionId.Value)
                : null;

            result.Add(MapToProjectFileWeb(fileDto, packageDto.Name, currentVersionDto, userDict, totalVersions, isOwnerView, sharedWithUserIds, sasUris));
        }

        return result;
    }

    private ProjectFileWeb MapToProjectFileWeb(
        ProjectFileCacheDto fileDto,
        string packageName,
        ProjectFileVersionDto? currentVersionDto,
        Dictionary<Guid, ProjectMemberUserInfo> userDict,
        int totalVersions,
        bool isOwnerView,
        List<Guid> sharedWithUserIds,
        FileVersionSasUriInfo? sasUris)
    {
        ProjectFileVersionWeb? currentVersionWeb = null;

        if (currentVersionDto != null && sasUris != null)
        {
            string createdByUserName = userDict.TryGetValue(currentVersionDto.CreatedByUserId, out ProjectMemberUserInfo? versionCreator)
                ? versionCreator.FullName
                : string.Empty;

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
            OwnerName = userDict.TryGetValue(fileDto.OwnerId, out ProjectMemberUserInfo? owner)
                ? owner.FullName
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
