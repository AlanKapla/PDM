using Business.Interfaces.Model;
using CQRS;
using Entities.Models;
using MediatR;
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
        private readonly ICurrentUser currentUser;
        private readonly ILogger<AddFileVersionCommentCommandHandler> logger;

        public AddFileVersionCommentCommandHandler(
            IRepository<ProjectFileVersionComment> commentRepo,
            ICurrentUser currentUser,
            ILogger<AddFileVersionCommentCommandHandler> logger)
        {
            this.commentRepo = commentRepo;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(AddFileVersionCommentCommand request, CancellationToken cancellationToken)
        {
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
