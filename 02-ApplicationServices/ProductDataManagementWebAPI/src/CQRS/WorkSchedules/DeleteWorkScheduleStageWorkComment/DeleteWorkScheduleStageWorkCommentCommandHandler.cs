using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.DeleteWorkScheduleStageWorkComment
{
    public sealed class DeleteWorkScheduleStageWorkCommentCommandHandler : IRequestHandler<DeleteWorkScheduleStageWorkCommentCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStageWorkComment> commentRepository;
        private readonly ICurrentUser currentUser;
        private readonly IWorkScheduleCacheService scheduleCache;
        private readonly IWorkScheduleAccessService accessService;

        public DeleteWorkScheduleStageWorkCommentCommandHandler(
            IRepository<WorkScheduleStageWorkComment> commentRepository,
            ICurrentUser currentUser,
            IWorkScheduleCacheService scheduleCache,
            IWorkScheduleAccessService accessService)
        {
            this.commentRepository = commentRepository;
            this.currentUser = currentUser;
            this.scheduleCache = scheduleCache;
            this.accessService = accessService;
        }

        public async Task<Unit> Handle(DeleteWorkScheduleStageWorkCommentCommand request, CancellationToken cancellationToken)
        {
            WorkScheduleStageWorkComment comment = await commentRepository.GetFirstBySearch(
                c => c.Id == request.CommentId
                  && c.TenantId == request.TenantId
                  && c.Work.ProjectId == request.ProjectId
                  && c.Work.Stage.WorkScheduleId == request.WorkScheduleId)
                ?? throw new NotFoundApiException(nameof(WorkScheduleStageWorkComment), request.CommentId.ToString());

            bool isAuthor = comment.CreatedByUserId == currentUser.Id;
            if (!isAuthor)
            {
                await accessService.RequireAdminOrOwnerAsync(request.TenantId, request.ProjectId, request.WorkScheduleId, cancellationToken);
            }

            await commentRepository.Delete(comment);
            await commentRepository.SaveChangesAsync(cancellationToken);
            await scheduleCache.InvalidateScheduleAsync(request.WorkScheduleId, cancellationToken);
            return Unit.Value;
        }
    }
}
