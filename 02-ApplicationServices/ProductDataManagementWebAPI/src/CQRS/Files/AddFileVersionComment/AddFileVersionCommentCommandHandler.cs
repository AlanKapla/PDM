using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
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
        private readonly IFileAccessGuard fileAccessGuard;
        private readonly IProjectFilesService projectFilesService;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<AddFileVersionCommentCommandHandler> logger;

        public AddFileVersionCommentCommandHandler(
            IRepository<ProjectFileVersionComment> commentRepo,
            IReadRepository<ProjectFileVersion> projectFileVersionRepo,
            IFileAccessGuard fileAccessGuard,
            IProjectFilesService projectFilesService,
            ICurrentUser currentUser,
            ILogger<AddFileVersionCommentCommandHandler> logger)
        {
            this.commentRepo = commentRepo;
            this.projectFileVersionRepo = projectFileVersionRepo;
            this.fileAccessGuard = fileAccessGuard;
            this.projectFilesService = projectFilesService;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(AddFileVersionCommentCommand request, CancellationToken cancellationToken)
        {
            // 1. Authorization (NotFound when file missing, Forbidden when caller lacks access)
            await fileAccessGuard.EnsureCanAccessFileAsync(
                request.TenantId, request.ProjectId, request.FileId, FileAccessKind.Write, cancellationToken);

            // 2. Verify file version exists and belongs to the file
            ProjectFileVersion fileVersion = await projectFileVersionRepo.GetFirstBySearch(
                pfv => pfv.Id == request.VersionId
                    && pfv.TenantId == request.TenantId
                    && pfv.ProjectId == request.ProjectId)
                ?? throw new NotFoundApiException(nameof(ProjectFileVersion), request.VersionId.ToString());

            if (fileVersion.ProjectFileId != request.FileId)
            {
                throw new NotFoundApiException(nameof(ProjectFileVersion), request.VersionId.ToString());
            }

            // 3. Create and save comment
            ProjectFileVersionComment comment = new ProjectFileVersionComment
            {
                ProjectFileVersionId = request.VersionId,
                ProjectId = request.ProjectId,
                UserId = currentUser.Id,
                TenantId = request.TenantId,
                Content = request.Comment,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await commentRepo.Insert(comment);

            // Invalidate comments cache
            await projectFilesService.InvalidateProjectCommentsCacheAsync(request.TenantId, request.ProjectId, cancellationToken);

            logger.LogInformation(
                "Comment added to file version {VersionId} of file {FileId} in project {ProjectId} by user {UserId}",
                request.VersionId, request.FileId, request.ProjectId, currentUser.Id);

            return Unit.Value;
        }
    }
}
