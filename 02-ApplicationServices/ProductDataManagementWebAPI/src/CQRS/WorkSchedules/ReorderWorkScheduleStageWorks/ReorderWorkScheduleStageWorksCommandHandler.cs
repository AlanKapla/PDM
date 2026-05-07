using Business.Interfaces.Exceptions;
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

namespace CQRS.WorkSchedules.ReorderWorkScheduleStageWorks
{
    public sealed class ReorderWorkScheduleStageWorksCommandHandler : IRequestHandler<ReorderWorkScheduleStageWorksCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStageWork> workRepo;
        private readonly IWorkScheduleCacheService scheduleCache;
        private readonly IWorkScheduleAccessService accessService;

        public ReorderWorkScheduleStageWorksCommandHandler(
            IRepository<WorkScheduleStageWork> workRepo,
            IWorkScheduleCacheService scheduleCache,
            IWorkScheduleAccessService accessService)
        {
            this.workRepo = workRepo;
            this.scheduleCache = scheduleCache;
            this.accessService = accessService;
        }

        public async Task<Unit> Handle(ReorderWorkScheduleStageWorksCommand request, CancellationToken cancellationToken)
        {
            await accessService.RequireAdminOrOwnerAsync(request.TenantId, request.ProjectId, request.WorkScheduleId, cancellationToken);

            IEnumerable<WorkScheduleStageWork> worksRaw = await workRepo.GetBySearch(
                w => w.WorkScheduleStageId == request.WorkScheduleStageId
                  && w.TenantId == request.TenantId
                  && w.ProjectId == request.ProjectId);

            Dictionary<Guid, WorkScheduleStageWork> workMap = worksRaw.ToDictionary(w => w.Id);
            HashSet<Guid> orderedWorkIds = request.OrderedWorkIds.ToHashSet();

            if (request.OrderedWorkIds.Count != workMap.Count
                || orderedWorkIds.Count != workMap.Count
                || !orderedWorkIds.SetEquals(workMap.Keys))
            {
                throw new ValidationApiException("OrderedWorkIds must contain exactly all works from the stage.");
            }

            for (int i = 0; i < request.OrderedWorkIds.Count; i++)
            {
                workMap[request.OrderedWorkIds[i]].Order = i;
            }

            List<WorkScheduleStageWork> worksToUpdate = request.OrderedWorkIds
                .Select(id => workMap[id])
                .ToList();

            await workRepo.UpdateRange(worksToUpdate);
            await workRepo.SaveChangesAsync(cancellationToken);
            await scheduleCache.InvalidateScheduleAsync(request.WorkScheduleId, cancellationToken);
            return Unit.Value;
        }
    }
}
