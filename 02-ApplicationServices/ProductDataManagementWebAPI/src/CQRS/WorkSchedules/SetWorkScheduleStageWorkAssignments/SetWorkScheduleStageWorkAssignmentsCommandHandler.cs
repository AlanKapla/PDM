using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models.WorkSchedules;
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
        private readonly IContractorService contractorService;
        private readonly IWorkScheduleNotificationService notificationService;
        private readonly IWorkScheduleCacheService scheduleCache;
        private readonly IWorkScheduleAccessService accessService;

        public SetWorkScheduleStageWorkAssignmentsCommandHandler(
            IRepository<WorkScheduleStageWork> workRepository,
            IRepository<WorkScheduleStageWorkAssignment> assignmentRepository,
            IRepository<WorkSchedule> workScheduleRepo,
            IProjectMemberService projectMemberService,
            IContractorService contractorService,
            IWorkScheduleNotificationService notificationService,
            IWorkScheduleCacheService scheduleCache,
            IWorkScheduleAccessService accessService)
        {
            this.workRepository = workRepository;
            this.assignmentRepository = assignmentRepository;
            this.workScheduleRepo = workScheduleRepo;
            this.projectMemberService = projectMemberService;
            this.contractorService = contractorService;
            this.notificationService = notificationService;
            this.scheduleCache = scheduleCache;
            this.accessService = accessService;
        }

        public async Task<Unit> Handle(SetWorkScheduleStageWorkAssignmentsCommand request, CancellationToken cancellationToken)
        {
            await EnsureWorkExistsAsync(request, cancellationToken);
            await accessService.RequireAdminOrOwnerAsync(request.TenantId, request.ProjectId, request.WorkScheduleId, cancellationToken);
            await ValidateAssigneesAsync(request, cancellationToken);

            IEnumerable<WorkScheduleStageWorkAssignment> existing = await assignmentRepository.GetBySearch(
                a => a.WorkScheduleStageWorkId == request.WorkScheduleStageWorkId);

            HashSet<Guid> existingUserIds = existing
                .Where(a => a.UserId.HasValue)
                .Select(a => a.UserId!.Value)
                .ToHashSet();
            HashSet<Guid> existingContractorIds = existing
                .Where(a => a.ContractorId.HasValue)
                .Select(a => a.ContractorId!.Value)
                .ToHashSet();

            HashSet<Guid> incomingUserIds = request.UserIds.ToHashSet();
            HashSet<Guid> incomingContractorIds = request.ContractorIds.ToHashSet();

            HashSet<Guid> removedUserIds = existingUserIds.Except(incomingUserIds).ToHashSet();
            HashSet<Guid> addedUserIds = incomingUserIds.Except(existingUserIds).ToHashSet();
            HashSet<Guid> removedContractorIds = existingContractorIds.Except(incomingContractorIds).ToHashSet();
            HashSet<Guid> addedContractorIds = incomingContractorIds.Except(existingContractorIds).ToHashSet();

            await RemoveAssignmentsAsync(request.WorkScheduleStageWorkId, removedUserIds, removedContractorIds, cancellationToken);
            await AddAssignmentsAsync(request, addedUserIds, addedContractorIds, cancellationToken);
            await NotifyUserAssignmentChangesAsync(request, removedUserIds, addedUserIds, cancellationToken);

            await scheduleCache.InvalidateWorkAsync(request.WorkScheduleId, request.WorkScheduleStageWorkId, cancellationToken);
            return Unit.Value;
        }

        private async Task EnsureWorkExistsAsync(
            SetWorkScheduleStageWorkAssignmentsCommand request,
            CancellationToken cancellationToken)
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
        }

        private async Task ValidateAssigneesAsync(
            SetWorkScheduleStageWorkAssignmentsCommand request,
            CancellationToken cancellationToken)
        {
            if (request.UserIds.Count > 0)
            {
                bool allMembers = await projectMemberService.AreAllMembersOfProjectAsync(
                    request.ProjectId,
                    request.UserIds,
                    cancellationToken);

                if (!allMembers)
                {
                    throw new ValidationApiException("One or more users are not members of this project.");
                }
            }

            if (request.ContractorIds.Count > 0)
            {
                bool allContractors = await contractorService.AreAllInTenantAsync(
                    request.TenantId,
                    request.ContractorIds,
                    cancellationToken);

                if (!allContractors)
                {
                    throw new ValidationApiException("One or more contractors do not belong to this tenant.");
                }
            }
        }

        private async Task RemoveAssignmentsAsync(
            Guid workId,
            HashSet<Guid> removedUserIds,
            HashSet<Guid> removedContractorIds,
            CancellationToken cancellationToken)
        {
            if (removedUserIds.Count > 0)
            {
                await assignmentRepository.ExecuteDeleteAsync(
                    a => a.WorkScheduleStageWorkId == workId
                      && a.UserId.HasValue
                      && removedUserIds.Contains(a.UserId.Value),
                    cancellationToken);
            }

            if (removedContractorIds.Count > 0)
            {
                await assignmentRepository.ExecuteDeleteAsync(
                    a => a.WorkScheduleStageWorkId == workId
                      && a.ContractorId.HasValue
                      && removedContractorIds.Contains(a.ContractorId.Value),
                    cancellationToken);
            }
        }

        private async Task AddAssignmentsAsync(
            SetWorkScheduleStageWorkAssignmentsCommand request,
            HashSet<Guid> addedUserIds,
            HashSet<Guid> addedContractorIds,
            CancellationToken cancellationToken)
        {
            List<WorkScheduleStageWorkAssignment> newAssignments = new();

            foreach (Guid userId in addedUserIds)
            {
                newAssignments.Add(new WorkScheduleStageWorkAssignment
                {
                    WorkScheduleStageWorkId = request.WorkScheduleStageWorkId,
                    TenantId = request.TenantId,
                    ProjectId = request.ProjectId,
                    UserId = userId,
                    ContractorId = null
                });
            }

            foreach (Guid contractorId in addedContractorIds)
            {
                newAssignments.Add(new WorkScheduleStageWorkAssignment
                {
                    WorkScheduleStageWorkId = request.WorkScheduleStageWorkId,
                    TenantId = request.TenantId,
                    ProjectId = request.ProjectId,
                    UserId = null,
                    ContractorId = contractorId
                });
            }

            if (newAssignments.Count == 0)
            {
                return;
            }

            await assignmentRepository.InsertRange(newAssignments);
            await assignmentRepository.SaveChangesAsync(cancellationToken);
        }

        private async Task NotifyUserAssignmentChangesAsync(
            SetWorkScheduleStageWorkAssignmentsCommand request,
            HashSet<Guid> removedUserIds,
            HashSet<Guid> addedUserIds,
            CancellationToken cancellationToken)
        {
            if (removedUserIds.Count == 0 && addedUserIds.Count == 0)
            {
                return;
            }

            WorkSchedule? workSchedule = await workScheduleRepo.GetFirstBySearch(
                ws => ws.Id == request.WorkScheduleId);

            if (workSchedule is null)
            {
                return;
            }

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
}
