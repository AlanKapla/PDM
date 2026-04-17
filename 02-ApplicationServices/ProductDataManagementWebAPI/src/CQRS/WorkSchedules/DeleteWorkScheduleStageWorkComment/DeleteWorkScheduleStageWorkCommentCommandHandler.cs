using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.DeleteWorkScheduleStageWorkComment
{
    public sealed class DeleteWorkScheduleStageWorkCommentCommandHandler : IRequestHandler<DeleteWorkScheduleStageWorkCommentCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStageWorkComment> commentRepository;
        private readonly ICurrentUser currentUser;

        public DeleteWorkScheduleStageWorkCommentCommandHandler(
            IRepository<WorkScheduleStageWorkComment> commentRepository,
            ICurrentUser currentUser)
        {
            this.commentRepository = commentRepository;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(DeleteWorkScheduleStageWorkCommentCommand request, CancellationToken cancellationToken)
        {
            WorkScheduleStageWorkComment comment = await commentRepository.GetFirstBySearch(
                c => c.Id == request.CommentId
                  && c.TenantId == request.TenantId)
                ?? throw new NotFoundApiException(nameof(WorkScheduleStageWorkComment), request.CommentId.ToString());

            if (comment.CreatedByUserId != currentUser.Id)
            {
                throw new ForbiddenApiException("Access denied.");
            }

            await commentRepository.Delete(comment);
            await commentRepository.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
