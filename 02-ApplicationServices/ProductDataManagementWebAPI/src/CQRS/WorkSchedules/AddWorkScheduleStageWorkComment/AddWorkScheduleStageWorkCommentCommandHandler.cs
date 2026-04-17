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
        private readonly ICurrentUser currentUser;
        private readonly IWorkScheduleCacheService scheduleCache;
        private readonly IWorkScheduleAccessService accessService;

        public AddWorkScheduleStageWorkCommentCommandHandler(
            IRepository<WorkScheduleStageWork> workRepository,
            IRepository<WorkScheduleStageWorkComment> commentRepository,
            ICurrentUser currentUser,
            IWorkScheduleCacheService scheduleCache,
            IWorkScheduleAccessService accessService)
        {
            this.workRepository = workRepository;
            this.commentRepository = commentRepository;
            this.currentUser = currentUser;
            this.scheduleCache = scheduleCache;
            this.accessService = accessService;
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

            await accessService.RequireAdminOwnerOrAssignedAsync(request.TenantId, request.ProjectId, request.WorkScheduleId, request.WorkScheduleStageWorkId, cancellationToken);

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
