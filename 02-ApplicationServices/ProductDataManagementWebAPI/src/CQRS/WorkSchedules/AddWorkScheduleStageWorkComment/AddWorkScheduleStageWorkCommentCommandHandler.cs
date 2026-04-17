using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.AddWorkScheduleStageWorkComment
{
    public sealed class AddWorkScheduleStageWorkCommentCommandHandler : IRequestHandler<AddWorkScheduleStageWorkCommentCommand, Guid>
    {
        private readonly IRepository<WorkScheduleStageWork> workRepository;
        private readonly IRepository<WorkScheduleStageWorkComment> commentRepository;
        private readonly IRepository<WorkScheduleStageWorkAssignment> assignmentRepository;
        private readonly ICurrentUser currentUser;
        private readonly IWorkScheduleCacheService scheduleCache;

        public AddWorkScheduleStageWorkCommentCommandHandler(
            IRepository<WorkScheduleStageWork> workRepository,
            IRepository<WorkScheduleStageWorkComment> commentRepository,
            IRepository<WorkScheduleStageWorkAssignment> assignmentRepository,
            ICurrentUser currentUser,
            IWorkScheduleCacheService scheduleCache)
        {
            this.workRepository = workRepository;
            this.commentRepository = commentRepository;
            this.assignmentRepository = assignmentRepository;
            this.currentUser = currentUser;
            this.scheduleCache = scheduleCache;
        }

        public async Task<Guid> Handle(AddWorkScheduleStageWorkCommentCommand request, CancellationToken cancellationToken)
        {
            bool workExists = await workRepository.AnyAsync(
                w => w.Id == request.WorkScheduleStageWorkId
                  && w.TenantId == request.TenantId
                  && w.ProjectId == request.ProjectId,
                cancellationToken);

            if (!workExists)
            {
                throw new NotFoundApiException(nameof(WorkScheduleStageWork), request.WorkScheduleStageWorkId.ToString());
            }

            bool isAssigned = await assignmentRepository.AnyAsync(
                a => a.WorkScheduleStageWorkId == request.WorkScheduleStageWorkId
                  && a.UserId == currentUser.Id,
                cancellationToken);

            if (!isAssigned)
            {
                throw new ForbiddenApiException("You are not assigned to this work item.");
            }

            WorkScheduleStageWorkComment comment = new WorkScheduleStageWorkComment
            {
                TenantId = request.TenantId,
                WorkScheduleStageWorkId = request.WorkScheduleStageWorkId,
                Content = request.Content,
                CreatedByUserId = currentUser.Id,
                CreatedAt = DateTime.UtcNow
            };

            await commentRepository.Insert(comment);
            await commentRepository.SaveChangesAsync(cancellationToken);
            await scheduleCache.InvalidateWorkAsync(request.WorkScheduleId, request.WorkScheduleStageWorkId, cancellationToken);
            return comment.Id;
        }
    }
}
