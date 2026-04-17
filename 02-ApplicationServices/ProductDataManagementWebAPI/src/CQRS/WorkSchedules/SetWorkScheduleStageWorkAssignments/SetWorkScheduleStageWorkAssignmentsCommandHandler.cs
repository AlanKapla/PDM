using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkAssignments
{
    public sealed class SetWorkScheduleStageWorkAssignmentsCommandHandler : IRequestHandler<SetWorkScheduleStageWorkAssignmentsCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStageWork> workRepository;
        private readonly IRepository<WorkScheduleStageWorkAssignment> assignmentRepository;
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly IProjectMemberService projectMemberService;
        private readonly IWorkScheduleNotificationService notificationService;
        private readonly IWorkScheduleCacheService scheduleCache;

        public SetWorkScheduleStageWorkAssignmentsCommandHandler(
            IRepository<WorkScheduleStageWork> workRepository,
            IRepository<WorkScheduleStageWorkAssignment> assignmentRepository,
            IRepository<WorkSchedule> workScheduleRepo,
            IProjectMemberService projectMemberService,
            IWorkScheduleNotificationService notificationService,
            IWorkScheduleCacheService scheduleCache)
        {
            this.workRepository = workRepository;
            this.assignmentRepository = assignmentRepository;
            this.workScheduleRepo = workScheduleRepo;
            this.projectMemberService = projectMemberService;
            this.notificationService = notificationService;
            this.scheduleCache = scheduleCache;
        }

        public async Task<Unit> Handle(SetWorkScheduleStageWorkAssignmentsCommand request, CancellationToken cancellationToken)
        {
            bool workExists = await workRepository.AnyAsync(
                w => w.Id == request.WorkScheduleStageWorkId
                  && w.TenantId == request.TenantId
                  && w.ProjectId == request.ProjectId,
                cancellationToken);

            if (!workExists)
                throw new NotFoundApiException(nameof(WorkScheduleStageWork), request.WorkScheduleStageWorkId.ToString());

            if (request.UserIds.Count > 0)
            {
                bool allMembers = await projectMemberService.AreAllMembersOfProjectAsync(
                    request.ProjectId,
                    request.UserIds,
                    cancellationToken);

                if (!allMembers)
                    throw new ValidationApiException("One or more users are not members of this project.");
            }

            IEnumerable<WorkScheduleStageWorkAssignment> existing = await assignmentRepository.GetBySearch(
                a => a.WorkScheduleStageWorkId == request.WorkScheduleStageWorkId);

            HashSet<Guid> existingUserIds = existing.Select(a => a.UserId).ToHashSet();
            HashSet<Guid> incomingUserIds = request.UserIds.ToHashSet();

            HashSet<Guid> removedUserIds = existingUserIds.Except(incomingUserIds).ToHashSet();
            HashSet<Guid> addedUserIds = incomingUserIds.Except(existingUserIds).ToHashSet();

            if (removedUserIds.Count > 0)
            {
                await assignmentRepository.ExecuteDeleteAsync(
                    a => a.WorkScheduleStageWorkId == request.WorkScheduleStageWorkId
                      && removedUserIds.Contains(a.UserId),
                    cancellationToken);
            }

            if (addedUserIds.Count > 0)
            {
                List<WorkScheduleStageWorkAssignment> newAssignments = addedUserIds
                    .Select(userId => new WorkScheduleStageWorkAssignment
                    {
                        WorkScheduleStageWorkId = request.WorkScheduleStageWorkId,
                        TenantId = request.TenantId,
                        ProjectId = request.ProjectId,
                        UserId = userId
                    })
                    .ToList();

                await assignmentRepository.InsertRange(newAssignments);
                await assignmentRepository.SaveChangesAsync(cancellationToken);
            }

            if (removedUserIds.Count > 0 || addedUserIds.Count > 0)
            {
                WorkSchedule? workSchedule = await workScheduleRepo.GetFirstBySearch(
                    ws => ws.Id == request.WorkScheduleId && !ws.IsDeleted);

                if (workSchedule is not null)
                {
                    await notificationService.SendAssignmentChangedNotificationsAsync(
                        removedUserIds,
                        addedUserIds,
                        request.WorkScheduleId,
                        workSchedule.Name,
                        request.TenantId,
                        request.ProjectId,
                        cancellationToken);
                }
            }

            await scheduleCache.InvalidateWorkAsync(request.WorkScheduleId, request.WorkScheduleStageWorkId, cancellationToken);
            return Unit.Value;
        }
    }
}
