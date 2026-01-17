using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Files;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.GetVersionComments;

public class GetVersionCommentsQueryHandler : IRequestHandler<GetVersionCommentsQuery, List<ProjectFileVersionCommentWeb>>
{
    private readonly IRepository<ProjectFile> fileRepo;
    private readonly IRepository<ProjectFileVersion> versionRepo;
    private readonly IRepository<ProjectFileVersionComment> commentRepo;
    private readonly IRepository<SharedProjectFile> sharedProjectFileRepo;
    private readonly ICurrentUser currentUser;

    public GetVersionCommentsQueryHandler(
        IRepository<ProjectFile> fileRepo,
        IRepository<ProjectFileVersion> versionRepo,
        IRepository<ProjectFileVersionComment> commentRepo,
        IRepository<SharedProjectFile> sharedProjectFileRepo,
        ICurrentUser currentUser)
    {
        this.fileRepo = fileRepo;
        this.versionRepo = versionRepo;
        this.commentRepo = commentRepo;
        this.sharedProjectFileRepo = sharedProjectFileRepo;
        this.currentUser = currentUser;
    }

    public async Task<List<ProjectFileVersionCommentWeb>> Handle(GetVersionCommentsQuery request, CancellationToken cancellationToken)
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
            return new List<ProjectFileVersionCommentWeb>();
        }

        // Verify version exists and belongs to file
        var versions = await versionRepo.GetBySearch(
            v => v.Id == request.VersionId &&
                 v.ProjectFileId == request.FileId &&
                 !v.IsDeleted
        );

        if (!versions.Any())
        {
            throw new NotFoundApiException(nameof(ProjectFileVersion), request.VersionId.ToString());
        }

        // Get comments with user
        var comments = await commentRepo.GetBySearch(
            c => c.ProjectFileVersionId == request.VersionId && !c.IsDeleted,
            include => include.Include(c => c.User)
        );

        return comments
            .OrderBy(c => c.CreatedAt)
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
}
