using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Files;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.GetVersionComments;

public class GetVersionCommentsQueryHandler : IRequestHandler<GetVersionCommentsQuery, List<ProjectFileVersionCommentWeb>>
{
    private readonly IProjectFilesService projectFilesService;
    private readonly IFileAccessService fileAccessService;
    private readonly IReadRepository<User> userRepository;
    private readonly ICurrentUser currentUser;

    public GetVersionCommentsQueryHandler(
        IProjectFilesService projectFilesService,
        IFileAccessService fileAccessService,
        IReadRepository<User> userRepository,
        ICurrentUser currentUser)
    {
        this.projectFilesService = projectFilesService;
        this.fileAccessService = fileAccessService;
        this.userRepository = userRepository;
        this.currentUser = currentUser;
    }

    public async Task<List<ProjectFileVersionCommentWeb>> Handle(GetVersionCommentsQuery request, CancellationToken cancellationToken)
    {
        // Get file from cache
        Dictionary<Guid, List<ProjectFileCacheDto>> allFilesByPackage = await projectFilesService.GetProjectPackageFilesAsync(
            request.TenantId,
            request.ProjectId,
            cancellationToken);

        ProjectFileCacheDto? fileDto = allFilesByPackage.Values
            .SelectMany(files => files)
            .FirstOrDefault(f => f.Id == request.FileId);

        if (fileDto == null)
        {
            throw new NotFoundApiException(nameof(ProjectFile), request.FileId.ToString());
        }

        // Check access
        bool hasAccess = await HasAccessToFileAsync(
            fileDto,
            request.Scope,
            request.TenantId,
            request.ProjectId,
            cancellationToken);

        if (!hasAccess)
        {
            return new List<ProjectFileVersionCommentWeb>();
        }

        // Verify version exists in cache
        Dictionary<Guid, List<ProjectFileVersionDto>> allVersionsByFile = await projectFilesService.GetProjectFilesVersionsAsync(
            request.TenantId,
            request.ProjectId,
            cancellationToken);

        if (!allVersionsByFile.TryGetValue(request.FileId, out List<ProjectFileVersionDto>? versionDtos))
        {
            throw new NotFoundApiException(nameof(ProjectFileVersion), request.VersionId.ToString());
        }

        ProjectFileVersionDto? versionDto = versionDtos.FirstOrDefault(v => v.Id == request.VersionId);
        if (versionDto == null)
        {
            throw new NotFoundApiException(nameof(ProjectFileVersion), request.VersionId.ToString());
        }

        // Get comments from cache
        Dictionary<Guid, List<ProjectFileVersionCommentDto>> allCommentsByVersion = await projectFilesService.GetProjectFileVersionsCommentsAsync(
            request.TenantId,
            request.ProjectId,
            cancellationToken);

        if (!allCommentsByVersion.TryGetValue(request.VersionId, out List<ProjectFileVersionCommentDto>? commentDtos))
        {
            return new List<ProjectFileVersionCommentWeb>();
        }

        // Get unique UserIds and fetch users as dictionary
        HashSet<Guid> userIds = commentDtos.Select(c => c.UserId).ToHashSet();

        Dictionary<Guid, User> userDict = await userRepository.GetDictionaryBySearchAsync(
            u => userIds.Contains(u.Id),
            cancellationToken);

        return commentDtos
            .OrderBy(c => c.CreatedAt)
            .Select(c =>
            {
                string userName = string.Empty;
                if (userDict.TryGetValue(c.UserId, out User? user))
                {
                    userName = $"{user.FirstName} {user.LastName}".Trim();
                }

                return new ProjectFileVersionCommentWeb
                {
                    Id = c.Id,
                    ProjectFileVersionId = c.ProjectFileVersionId,
                    UserId = c.UserId,
                    UserName = userName,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    EditedAt = c.EditedAt,
                    IsEdited = c.EditedAt.HasValue,
                    CanEdit = c.UserId == currentUser.Id,
                    CanDelete = c.UserId == currentUser.Id
                };
            })
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

        // ResourceScope.Shared - use FileAccessService
        return await fileAccessService.HasAccessToFileAsync(
            currentUser,
            fileDto.ProjectFilePackageId,
            fileDto.Id,
            scope,
            cancellationToken);
    }
}
