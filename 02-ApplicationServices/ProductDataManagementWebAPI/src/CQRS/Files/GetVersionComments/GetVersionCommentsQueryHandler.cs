using Business.Implementation.Services.Files;
using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Files;
using Entities.Models.Files;
using MediatR;

namespace CQRS.Files.GetVersionComments;

public sealed class GetVersionCommentsQueryHandler : IRequestHandler<GetVersionCommentsQuery, List<ProjectFileVersionCommentWeb>>
{
    private readonly IProjectFilesService projectFilesService;
    private readonly IUserService userService;
    private readonly ICurrentUser currentUser;

    public GetVersionCommentsQueryHandler(
        IProjectFilesService projectFilesService,
        IUserService userService,
        ICurrentUser currentUser)
    {
        this.projectFilesService = projectFilesService;
        this.userService = userService;
        this.currentUser = currentUser;
    }

    public async Task<List<ProjectFileVersionCommentWeb>> Handle(GetVersionCommentsQuery request, CancellationToken cancellationToken)
    {
        _ = await projectFilesService.GetAccessibleFileByIdAsync(
            currentUser, request.TenantId, request.ProjectId, request.FileId, request.Scope, cancellationToken)
            ?? throw new NotFoundApiException(nameof(ProjectFile), request.FileId.ToString());

        _ = await projectFilesService.GetFileVersionByIdAsync(
            request.TenantId, request.ProjectId, request.FileId, request.VersionId, cancellationToken)
            ?? throw new NotFoundApiException(nameof(ProjectFileVersion), request.VersionId.ToString());

        List<ProjectFileVersionCommentDto> commentDtos = await projectFilesService.GetVersionCommentsAsync(
            request.TenantId, request.ProjectId, request.VersionId, cancellationToken);

        if (commentDtos.Count == 0)
        {
            return new List<ProjectFileVersionCommentWeb>();
        }

        HashSet<Guid> userIds = commentDtos.Select(c => c.UserId).ToHashSet();

        Dictionary<Guid, ProjectMemberUserInfo> userDict = await userService.GetProjectMembersByIdsAsync(
            request.TenantId, request.ProjectId, userIds, cancellationToken);

        return commentDtos
            .OrderBy(c => c.CreatedAt)
            .Select(c => MapToCommentWeb(c, userDict))
            .ToList();
    }

    private ProjectFileVersionCommentWeb MapToCommentWeb(
        ProjectFileVersionCommentDto comment,
        IReadOnlyDictionary<Guid, ProjectMemberUserInfo> userDict)
    {
        return new ProjectFileVersionCommentWeb
        {
            Id = comment.Id,
            ProjectFileVersionId = comment.ProjectFileVersionId,
            UserId = comment.UserId,
            UserName = ProjectMemberNameResolver.ResolveUserName(userDict, comment.UserId),
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            EditedAt = comment.EditedAt,
            IsEdited = comment.EditedAt.HasValue,
            CanEdit = comment.UserId == currentUser.Id,
            CanDelete = comment.UserId == currentUser.Id
        };
    }
}
