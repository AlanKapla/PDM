using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Files;
using Entities.Models;
using MediatR;

namespace CQRS.Files.GetFileVersions;

public class GetFileVersionsQueryHandler : IRequestHandler<GetFileVersionsQuery, List<ProjectFileVersionWeb>>
{
    private readonly IProjectFilesService projectFilesService;
    private readonly IUserService userService;
    private readonly ICurrentUser currentUser;

    public GetFileVersionsQueryHandler(
        IProjectFilesService projectFilesService,
        IUserService userService,
        ICurrentUser currentUser)
    {
        this.projectFilesService = projectFilesService;
        this.userService = userService;
        this.currentUser = currentUser;
    }

    public async Task<List<ProjectFileVersionWeb>> Handle(GetFileVersionsQuery request, CancellationToken cancellationToken)
    {
        ProjectFileCacheDto? fileDto = await projectFilesService.GetAccessibleFileByIdAsync(
            currentUser, request.TenantId, request.ProjectId, request.FileId, request.Scope, cancellationToken)
            ?? throw new NotFoundApiException(nameof(ProjectFile), request.FileId.ToString());

        List<ProjectFileVersionDto> versionDtos = await projectFilesService.GetFileVersionsAsync(
            request.TenantId, request.ProjectId, request.FileId, cancellationToken);

        if (versionDtos.Count == 0)
        {
            return new List<ProjectFileVersionWeb>();
        }

        Guid[] versionIds = versionDtos.Select(v => v.Id).ToArray();

        Dictionary<Guid, FileVersionSasUriInfo> sasUrisDict = await projectFilesService.GetFileVersionsSasUrisAsync(
            request.TenantId, request.ProjectId, versionIds);

        HashSet<Guid> createdByUserIds = versionDtos.Select(v => v.CreatedByUserId).ToHashSet();

        Dictionary<Guid, ProjectMemberUserInfo> userDict = await userService.GetProjectMembersByIdsAsync(
            request.TenantId, request.ProjectId, createdByUserIds, cancellationToken);

        return versionDtos
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => MapToVersionWeb(v, userDict, sasUrisDict.GetValueOrDefault(v.Id)))
            .ToList();
    }

    private static ProjectFileVersionWeb MapToVersionWeb(
        ProjectFileVersionDto versionDto,
        Dictionary<Guid, ProjectMemberUserInfo> userDict,
        FileVersionSasUriInfo? sasUriInfo)
    {
        return new ProjectFileVersionWeb
        {
            Id = versionDto.Id,
            ProjectFileId = versionDto.ProjectFileId,
            VersionNumber = versionDto.VersionNumber,
            ContentType = versionDto.ContentType,
            FileSizeBytes = versionDto.FileSizeBytes,
            CreatedAt = versionDto.CreatedAt,
            CreatedByUserId = versionDto.CreatedByUserId,
            CreatedByUserName = userDict.TryGetValue(versionDto.CreatedByUserId, out ProjectMemberUserInfo? user)
                ? user.FullName
                : string.Empty,
            SasUrlView = sasUriInfo?.SasUriView ?? string.Empty,
            SasUrlDownload = sasUriInfo?.SasUriDownload ?? string.Empty,
            Comments = new List<ProjectFileVersionCommentWeb>()
        };
    }
}
