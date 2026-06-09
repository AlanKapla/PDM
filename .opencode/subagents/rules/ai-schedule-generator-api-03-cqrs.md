# API-03: CQRS — GenerateScheduleFromEstimateAICommand

## Zadanie
Utwórz nowy folder CQRS `GenerateScheduleFromEstimateAI` z trzema plikami:
1. `GenerateScheduleFromEstimateAICommand.cs` — command
2. `GenerateScheduleFromEstimateAICommandHandler.cs` — handler
3. `GenerateScheduleFromEstimateAICommandValidator.cs` — walidator

## Lokalizacja
`CQRS/WorkSchedules/GenerateScheduleFromEstimateAI/`

## Pliki

### 1. GenerateScheduleFromEstimateAICommand.cs
```csharp
using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.WorkSchedules.Shared;
using MediatR;

namespace CQRS.WorkSchedules.GenerateScheduleFromEstimateAI
{
    public sealed record GenerateScheduleFromEstimateAICommand : WorkScheduleCommandBase, IRequestCommand<WorkScheduleDetailsWeb>
    {
        public DateTime OverallStartDate { get; init; }
        public DateTime OverallEndDate { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectSchedule;
    }
}
```

### 2. GenerateScheduleFromEstimateAICommandHandler.cs
Handler powinien:
1. Sprawdzić dostęp (RequireAdminOrOwnerAsync)
2. Załadować WorkSchedule z CostEstimateId
3. Zweryfikować że CostEstimateId jest ustawiony
4. Wywołać `IWorkScheduleSyncService.SyncFromCostEstimateAsync()` — synchronizacja przed AI
5. Załadować wszystkie stage i work z repo
6. Przygotować StageInput i WorkInput dla AI
7. Wywołać `IWorkScheduleAIGeneratorService.GenerateScheduleAsync()`
8. Dla każdego worka z okresem: wywołać mediator.Send(new SetWorkScheduleStageWorkPeriodsCommand { ... })
9. Wywołać mediator.Send(new SetWorkScheduleDependenciesCommand { ... })
10. Unieważnić cache
11. Zbudować i zwrócić WorkScheduleDetailsWeb przez WorkScheduleBuilder

```csharp
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
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
        private readonly IWorkScheduleNotificationService notificationService;
        private readonly ICurrentUser currentUser;
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
            IWorkScheduleNotificationService notificationService,
            ICurrentUser currentUser,
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
            this.notificationService = notificationService;
            this.currentUser = currentUser;
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

            // 1. Access check
            await accessService.RequireAdminOrOwnerAsync(tenantId, projectId, workScheduleId, cancellationToken);

            // 2. Load schedule
            WorkSchedule workSchedule = await workScheduleRepo.GetFirstBySearch(
                ws => ws.Id == workScheduleId && ws.TenantId == tenantId && ws.ProjectId == projectId,
                include => include
                    .Include(ws => ws.Stages.Where(s => !s.IsDeleted))
                    .ThenInclude(s => s.Works.Where(w => !w.IsDeleted)))
                ?? throw new NotFoundApiException(nameof(WorkSchedule), workScheduleId.ToString());

            // 3. Verify linked to cost estimate
            if (!workSchedule.CostEstimateId.HasValue)
            {
                throw new ValidationApiException("Work schedule is not linked to a cost estimate. Please sync with a cost estimate first.");
            }

            // 4. Sync with cost estimate first (ensures latest structure)
            await workScheduleSyncService.SyncFromCostEstimateAsync(workSchedule, cancellationToken);

            // 5. Reload stages and works after sync
            List<WorkScheduleStage> allStages = (await stageRepo.GetBySearch(
                s => s.WorkScheduleId == workScheduleId && !s.IsDeleted))
                .ToList();

            List<WorkScheduleStageWork> allWorks = (await workRepo.GetBySearch(
                w => w.WorkScheduleStageId != Guid.Empty // will filter by stages below
                      && !w.IsDeleted))
                .ToList();

            // Filter works to only those belonging to our stages
            HashSet<Guid> stageIds = allStages.Select(s => s.Id).ToHashSet();
            allWorks = allWorks.Where(w => stageIds.Contains(w.WorkScheduleStageId)).ToList();

            // 6. Prepare inputs for AI
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

            if (workInputs.Count == 0)
            {
                throw new ValidationApiException(
                    "No work items found after synchronization. The cost estimate has no items marked as work scope.");
            }

            // 7. Call AI to generate schedule
            AIScheduleResult aiResult = await aiGenerator.GenerateScheduleAsync(
                workScheduleId,
                tenantId,
                projectId,
                stageInputs,
                workInputs,
                request.OverallStartDate,
                request.OverallEndDate,
                cancellationToken);

            // 8. Save periods
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
                        new WorkPeriodDto
                        {
                            StartDate = period.StartDate,
                            EndDate = period.EndDate,
                            IsClosed = false
                        }
                    }
                };

                await mediator.Send(periodCommand, cancellationToken);
            }

            // 9. Save dependencies
            if (aiResult.Dependencies.Count > 0)
            {
                SetWorkScheduleDependenciesCommand depsCommand = new SetWorkScheduleDependenciesCommand
                {
                    TenantId = tenantId,
                    ProjectId = projectId,
                    WorkScheduleId = workScheduleId,
                    Dependencies = aiResult.Dependencies.Select(d => new WorkDependencyDto
                    {
                        PredecessorWorkId = d.PredecessorWorkId,
                        SuccessorWorkId = d.SuccessorWorkId,
                        DependencyType = d.DependencyType,
                        LagDays = d.LagDays
                    }).ToList()
                };

                await mediator.Send(depsCommand, cancellationToken);
            }

            // 10. Invalidate cache
            await scheduleCache.InvalidateScheduleAsync(workScheduleId, cancellationToken);

            // 11. Notify relevant users
            await notificationService.NotifyScheduleChangedAsync(workScheduleId, tenantId, projectId, cancellationToken);

            // 12. Build and return full schedule details
            WorkScheduleDetailsWeb result = await workScheduleBuilder.BuildAsync(
                workScheduleId, tenantId, projectId, cancellationToken);

            return result;
        }
    }
}
```

### 3. GenerateScheduleFromEstimateAICommandValidator.cs
```csharp
using CQRS.Extensions;
using FluentValidation;

namespace CQRS.WorkSchedules.GenerateScheduleFromEstimateAI
{
    public sealed class GenerateScheduleFromEstimateAICommandValidator : AbstractValidator<GenerateScheduleFromEstimateAICommand>
    {
        public GenerateScheduleFromEstimateAICommandValidator()
        {
            RuleFor(x => x.TenantId).RequiredId();
            RuleFor(x => x.ProjectId).RequiredId();
            RuleFor(x => x.WorkScheduleId).RequiredId();

            RuleFor(x => x.OverallStartDate)
                .NotEmpty()
                .WithMessage("Overall start date is required.");

            RuleFor(x => x.OverallEndDate)
                .NotEmpty()
                .WithMessage("Overall end date is required.");

            RuleFor(x => x)
                .Must(x => x.OverallEndDate > x.OverallStartDate)
                .WithMessage("Overall end date must be after overall start date.")
                .Must(x => (x.OverallEndDate - x.OverallStartDate).TotalDays >= 1)
                .WithMessage("The overall time frame must be at least 1 day.");
        }
    }
}
```

### Uwagi:
- Kolejność: najpierw okresy (SetWorkScheduleStageWorkPeriodsCommand), potem zależności (SetWorkScheduleDependenciesCommand)
- Handler zależności sam dostosuje okresy sukcesorów (AdjustSuccessorPeriodsAsync), ale AI już powinno wygenerować zgodne dane
- Sync przed AI zapewnia że nazwy są aktualne
- Użyj istniejących DTO: `WorkPeriodDto` z `SetWorkScheduleStageWorkPeriods`, `WorkDependencyDto` z `SetWorkScheduleDependencies`
- Sprawdź namespace dla `WorkPeriodDto` — może być w `CQRS.WorkSchedules.SetWorkScheduleStageWorkPeriods`
- Sprawdź namespace dla `WorkDependencyDto` — może być w `CQRS.WorkSchedules.SetWorkScheduleDependencies`
