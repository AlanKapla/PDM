using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.WorkSchedules.SetWorkScheduleDependencies;
using CQRS.WorkSchedules.SetWorkScheduleStageWorkPeriods;
using CQRS.WorkSchedules.Shared;
using Entities.Models.WorkSchedules;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.GenerateScheduleFromEstimateAI
{
    public sealed class GenerateScheduleFromEstimateAICommandHandler : IRequestHandler<GenerateScheduleFromEstimateAICommand, WorkScheduleDetailsWeb>
    {
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly IRepository<WorkScheduleStage> stageRepo;
        private readonly IRepository<WorkScheduleStageWork> workRepo;
        private readonly IWorkScheduleSyncService workScheduleSyncService;
        private readonly IWorkScheduleAIGeneratorService aiGenerator;
        private readonly IWorkScheduleCacheService scheduleCache;
        private readonly IWorkScheduleAccessService accessService;
        private readonly IMediator mediator;
        private readonly WorkScheduleBuilder workScheduleBuilder;

        public GenerateScheduleFromEstimateAICommandHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            IRepository<WorkScheduleStage> stageRepo,
            IRepository<WorkScheduleStageWork> workRepo,
            IWorkScheduleSyncService workScheduleSyncService,
            IWorkScheduleAIGeneratorService aiGenerator,
            IWorkScheduleCacheService scheduleCache,
            IWorkScheduleAccessService accessService,
            IMediator mediator,
            WorkScheduleBuilder workScheduleBuilder)
        {
            this.workScheduleRepo = workScheduleRepo;
            this.stageRepo = stageRepo;
            this.workRepo = workRepo;
            this.workScheduleSyncService = workScheduleSyncService;
            this.aiGenerator = aiGenerator;
            this.scheduleCache = scheduleCache;
            this.accessService = accessService;
            this.mediator = mediator;
            this.workScheduleBuilder = workScheduleBuilder;
        }

        public async Task<WorkScheduleDetailsWeb> Handle(
            GenerateScheduleFromEstimateAICommand request,
            CancellationToken cancellationToken)
        {
            Guid tenantId = request.TenantId;
            Guid projectId = request.ProjectId;
            Guid workScheduleId = request.WorkScheduleId;

            await accessService.RequireAdminOrOwnerAsync(tenantId, projectId, workScheduleId, cancellationToken);

            WorkSchedule workSchedule = await workScheduleRepo.GetFirstBySearch(
                ws => ws.Id == workScheduleId && ws.TenantId == tenantId && ws.ProjectId == projectId,
                include => include
                    .Include(ws => ws.Stages.Where(s => !s.IsDeleted))
                    .ThenInclude(s => s.Works.Where(w => !w.IsDeleted)))
                ?? throw new NotFoundApiException(nameof(WorkSchedule), workScheduleId.ToString());

            if (!workSchedule.CostEstimateId.HasValue)
            {
                throw new ValidationApiException("Work schedule is not linked to a cost estimate. Please sync with a cost estimate first.");
            }

            await workScheduleSyncService.SyncFromCostEstimateAsync(workSchedule, cancellationToken);

            (List<WorkScheduleStage> allStages, List<WorkScheduleStageWork> allWorks) =
                await LoadStagesAndWorksAsync(tenantId, projectId, workScheduleId, cancellationToken);

            (List<StageInput> stageInputs, List<WorkInput> workInputs) = BuildWorkInputs(allStages, allWorks);

            if (workInputs.Count == 0)
            {
                throw new ValidationApiException(
                    "No work items found after synchronization. The cost estimate has no items marked as work scope.");
            }

            AIScheduleResult aiResult = await aiGenerator.GenerateScheduleAsync(
                workScheduleId,
                tenantId,
                projectId,
                stageInputs,
                workInputs,
                request.OverallStartDate,
                request.OverallEndDate,
                cancellationToken);

            await PersistPeriodsAsync(tenantId, projectId, workScheduleId, allWorks, aiResult, cancellationToken);
            await PersistDependenciesAsync(tenantId, projectId, workScheduleId, aiResult, cancellationToken);

            await scheduleCache.InvalidateScheduleAsync(workScheduleId, cancellationToken);

            return await workScheduleBuilder.BuildAsync(
                workScheduleId, tenantId, projectId, cancellationToken);
        }

        private async Task<(List<WorkScheduleStage> Stages, List<WorkScheduleStageWork> Works)> LoadStagesAndWorksAsync(
            Guid tenantId,
            Guid projectId,
            Guid workScheduleId,
            CancellationToken cancellationToken)
        {
            List<WorkScheduleStage> allStages = (await stageRepo.GetBySearch(
                s => s.WorkScheduleId == workScheduleId
                     && s.TenantId == tenantId
                     && s.ProjectId == projectId
                     && !s.IsDeleted))
                .ToList();

            HashSet<Guid> stageIds = allStages.Select(s => s.Id).ToHashSet();

            List<WorkScheduleStageWork> allWorks = (await workRepo.GetBySearch(
                w => w.TenantId == tenantId
                     && w.ProjectId == projectId
                     && stageIds.Contains(w.WorkScheduleStageId)
                     && !w.IsDeleted))
                .ToList();

            return (allStages, allWorks);
        }

        private static (List<StageInput> StageInputs, List<WorkInput> WorkInputs) BuildWorkInputs(
            List<WorkScheduleStage> allStages,
            List<WorkScheduleStageWork> allWorks)
        {
            List<StageInput> stageInputs = allStages.Select(s => new StageInput
            {
                Id = s.Id,
                ParentStageId = s.ParentStageId,
                Name = s.Name,
                Order = s.Order
            }).ToList();

            Dictionary<Guid, string> stageNameById = allStages.ToDictionary(s => s.Id, s => s.Name);

            List<WorkInput> workInputs = allWorks.Select(w => new WorkInput
            {
                Id = w.Id,
                StageId = w.WorkScheduleStageId,
                Name = w.Name,
                Order = w.Order,
                StageName = stageNameById.TryGetValue(w.WorkScheduleStageId, out string? stageName) ? stageName : string.Empty
            }).ToList();

            return (stageInputs, workInputs);
        }

        private async Task PersistPeriodsAsync(
            Guid tenantId,
            Guid projectId,
            Guid workScheduleId,
            List<WorkScheduleStageWork> allWorks,
            AIScheduleResult aiResult,
            CancellationToken cancellationToken)
        {
            foreach (WorkPeriodResult period in aiResult.Periods)
            {
                WorkScheduleStageWork? targetWork = allWorks.FirstOrDefault(w => w.Id == period.WorkScheduleStageWorkId);
                if (targetWork is null)
                {
                    continue;
                }

                SetWorkScheduleStageWorkPeriodsCommand periodCommand = new SetWorkScheduleStageWorkPeriodsCommand
                {
                    TenantId = tenantId,
                    ProjectId = projectId,
                    WorkScheduleId = workScheduleId,
                    WorkScheduleStageWorkId = period.WorkScheduleStageWorkId,
                    Periods = new List<WorkPeriodDto>
                    {
                        new WorkPeriodDto(period.StartDate, period.EndDate, false)
                    }
                };

                await mediator.Send(periodCommand, cancellationToken);
            }
        }

        private async Task PersistDependenciesAsync(
            Guid tenantId,
            Guid projectId,
            Guid workScheduleId,
            AIScheduleResult aiResult,
            CancellationToken cancellationToken)
        {
            SetWorkScheduleDependenciesCommand depsCommand = new SetWorkScheduleDependenciesCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                WorkScheduleId = workScheduleId,
                Dependencies = aiResult.Dependencies.Select(d => new WorkDependencyDto(
                    d.PredecessorWorkId,
                    d.SuccessorWorkId,
                    d.DependencyType,
                    d.LagDays)).ToList()
            };
            await mediator.Send(depsCommand, cancellationToken);
        }
    }
}
