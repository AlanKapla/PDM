using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using CQRS;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Files.AddFileVersionComment
{
    /// <summary>
    /// Handler for adding a comment to a specific file version
    /// </summary>
    public class AddFileVersionCommentCommandHandler : IRequestHandler<AddFileVersionCommentCommand, Unit>
    {
        private readonly IRepository<ProjectFileVersionComment> commentRepo;
        private readonly IRepository<ProjectFile> projectFileRepo;
        private readonly IRepository<ProjectFileVersion> projectFileVersionRepo;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<AddFileVersionCommentCommandHandler> logger;

        public AddFileVersionCommentCommandHandler(
            IRepository<ProjectFileVersionComment> commentRepo,
            IRepository<ProjectFile> projectFileRepo,
            IRepository<ProjectFileVersion> projectFileVersionRepo,
            ICurrentUser currentUser,
            ILogger<AddFileVersionCommentCommandHandler> logger)
        {
            this.commentRepo = commentRepo;
            this.projectFileRepo = projectFileRepo;
            this.projectFileVersionRepo = projectFileVersionRepo;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(AddFileVersionCommentCommand request, CancellationToken cancellationToken)
        {
            // 1. Verify file version exists
            var fileVersion = await projectFileVersionRepo.GetFirstBySearch(
                pfv => pfv.Id == request.VersionId && !pfv.IsDeleted)
                ?? throw new NotFoundApiException(nameof(ProjectFileVersion), request.VersionId.ToString());

            // 2. Verify file exists and belongs to the correct project/tenant
            var file = await projectFileRepo.GetFirstBySearch(
                pf => pf.Id == request.FileId
                    && pf.ProjectId == request.ProjectId
                    && pf.TenantId == request.TenantId
                    && !pf.IsDeleted,
                query => query.Include(pf => pf.SharedWith))
                ?? throw new NotFoundApiException(nameof(ProjectFile), request.FileId.ToString());

            // 3. Verify file version belongs to the file
            if (fileVersion.ProjectFileId != file.Id)
            {
                throw new NotFoundApiException(nameof(ProjectFileVersion), request.VersionId.ToString());
            }

            // 5. Authorization check: tenant admin OR project admin OR file owner OR user with share access
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(request.TenantId, request.ProjectId, cancellationToken);
            bool isFileOwner = file.OwnerId == currentUser.Id;
            bool hasShareAccess = file.SharedWith.Any(s => s.SharedWithUserId == currentUser.Id);
            
            if (!isAdmin && !isFileOwner && !hasShareAccess)
            {
                throw new NotFoundApiException(nameof(ProjectFileVersion), request.VersionId.ToString());
            }
            
            // 6. Create and save comment
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

            logger.LogInformation(
                "Comment added to file version {VersionId} of file {FileId} in project {ProjectId} by user {UserId}",
                request.VersionId, request.FileId, request.ProjectId, currentUser.Id);

            return Unit.Value;
        }
    }
}
