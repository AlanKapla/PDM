using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.PostCommit;
using Entities.Models.Files;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.AddFileVersionComment
{
    /// <summary>
    /// Handler for adding a comment to a specific file version
    /// </summary>
    public sealed class AddFileVersionCommentCommandHandler : IRequestHandler<AddFileVersionCommentCommand, Unit>
    {
        private readonly IRepository<ProjectFileVersionComment> commentRepo;
        private readonly IReadRepository<ProjectFileVersion> projectFileVersionRepo;
        private readonly IReadRepository<ProjectFile> projectFileRepo;
        private readonly IFileAccessGuard fileAccessGuard;
        private readonly IProjectFilesService projectFilesService;
        private readonly IFileActivityNotificationService activityNotifications;
        private readonly IPostCommitDispatcher postCommitDispatcher;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<AddFileVersionCommentCommandHandler> logger;

        public AddFileVersionCommentCommandHandler(
            IRepository<ProjectFileVersionComment> commentRepo,
            IReadRepository<ProjectFileVersion> projectFileVersionRepo,
            IReadRepository<ProjectFile> projectFileRepo,
            IFileAccessGuard fileAccessGuard,
            IProjectFilesService projectFilesService,
            IFileActivityNotificationService activityNotifications,
            IPostCommitDispatcher postCommitDispatcher,
            ICurrentUser currentUser,
            ILogger<AddFileVersionCommentCommandHandler> logger)
        {
            this.commentRepo = commentRepo;
            this.projectFileVersionRepo = projectFileVersionRepo;
            this.projectFileRepo = projectFileRepo;
            this.fileAccessGuard = fileAccessGuard;
            this.projectFilesService = projectFilesService;
            this.activityNotifications = activityNotifications;
            this.postCommitDispatcher = postCommitDispatcher;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(AddFileVersionCommentCommand request, CancellationToken cancellationToken)
        {
            await fileAccessGuard.EnsureCanAccessFileAsync(
                request.TenantId, request.ProjectId, request.FileId, FileAccessKind.Write, cancellationToken);

            ProjectFileVersion fileVersion = await GetAndValidateVersionAsync(request);
            ProjectFile file = await GetAndValidateFileAsync(request);

            ProjectFileVersionComment comment = BuildComment(request);
            await commentRepo.Insert(comment);
            await projectFilesService.InvalidateProjectCommentsCacheAsync(
                request.TenantId, request.ProjectId, cancellationToken);

            FileActivityNotificationContext notificationContext = BuildNotificationContext(
                request, file, fileVersion.Id, comment.Id);
            postCommitDispatcher.Enqueue(ct =>
                activityNotifications.NotifyCommentAddedAsync(notificationContext, ct));

            logger.LogInformation(
                "Comment added to file version {VersionId} of file {FileId} in project {ProjectId} by user {UserId}",
                request.VersionId, request.FileId, request.ProjectId, currentUser.Id);

            return Unit.Value;
        }

        private async Task<ProjectFileVersion> GetAndValidateVersionAsync(AddFileVersionCommentCommand request)
        {
            ProjectFileVersion? fileVersion = await projectFileVersionRepo.GetFirstBySearch(
                pfv => pfv.Id == request.VersionId
                    && pfv.TenantId == request.TenantId
                    && pfv.ProjectId == request.ProjectId);

            if (fileVersion is null || fileVersion.ProjectFileId != request.FileId)
            {
                throw new NotFoundApiException(nameof(ProjectFileVersion), request.VersionId.ToString());
            }

            return fileVersion;
        }

        private async Task<ProjectFile> GetAndValidateFileAsync(AddFileVersionCommentCommand request)
        {
            ProjectFile? file = await projectFileRepo.GetFirstBySearch(
                pf => pf.Id == request.FileId
                    && pf.TenantId == request.TenantId
                    && pf.ProjectId == request.ProjectId);

            if (file is null)
            {
                throw new NotFoundApiException(nameof(ProjectFile), request.FileId.ToString());
            }

            return file;
        }

        private ProjectFileVersionComment BuildComment(AddFileVersionCommentCommand request) =>
            new ProjectFileVersionComment
            {
                ProjectFileVersionId = request.VersionId,
                ProjectId = request.ProjectId,
                UserId = currentUser.Id,
                TenantId = request.TenantId,
                Content = request.Comment,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

        private FileActivityNotificationContext BuildNotificationContext(
            AddFileVersionCommentCommand request,
            ProjectFile file,
            Guid versionId,
            Guid commentId) =>
            new FileActivityNotificationContext
            {
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                FileId = file.Id,
                PackageId = file.ProjectFilePackageId,
                OwnerId = file.OwnerId,
                FileDisplayName = file.DisplayName,
                ActorName = $"{currentUser.FirstName} {currentUser.LastName}".Trim(),
                ActorUserId = currentUser.Id,
                VersionId = versionId,
                CommentId = commentId,
            };
    }
}
