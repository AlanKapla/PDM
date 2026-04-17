using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.UpdateWorkScheduleStageWorkComment
{
    public sealed class UpdateWorkScheduleStageWorkCommentCommandHandler : IRequestHandler<UpdateWorkScheduleStageWorkCommentCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStageWorkComment> commentRepository;
        private readonly ICurrentUser currentUser;
        private readonly IWorkScheduleCacheService scheduleCache;

        public UpdateWorkScheduleStageWorkCommentCommandHandler(
            IRepository<WorkScheduleStageWorkComment> commentRepository,
            ICurrentUser currentUser,
            IWorkScheduleCacheService scheduleCache)
        {
            this.commentRepository = commentRepository;
            this.currentUser = currentUser;
            this.scheduleCache = scheduleCache;
        }

        public async Task<Unit> Handle(UpdateWorkScheduleStageWorkCommentCommand request, CancellationToken cancellationToken)
        {
            WorkScheduleStageWorkComment comment = await commentRepository.GetFirstBySearch(
                c => c.Id == request.CommentId
                  && c.TenantId == request.TenantId)
                ?? throw new NotFoundApiException(nameof(WorkScheduleStageWorkComment), request.CommentId.ToString());

            if (comment.CreatedByUserId != currentUser.Id)
            {
                throw new ForbiddenApiException("Access denied.");
            }

            comment.Content = request.Content;

            await commentRepository.Update(comment);
            await commentRepository.SaveChangesAsync(cancellationToken);
            await scheduleCache.InvalidateScheduleAsync(request.WorkScheduleId, cancellationToken);
            return Unit.Value;
        }
    }
}
