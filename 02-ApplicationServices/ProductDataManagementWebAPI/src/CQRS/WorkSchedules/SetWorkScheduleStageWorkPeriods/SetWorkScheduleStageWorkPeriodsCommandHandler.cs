using Business.Interfaces.Exceptions;
using CQRS.WorkSchedules.Shared;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.WorkSchedules.SetWorkScheduleStageWorkPeriods
{
    public sealed class SetWorkScheduleStageWorkPeriodsCommandHandler : IRequestHandler<SetWorkScheduleStageWorkPeriodsCommand, Unit>
    {
        private readonly IRepository<WorkScheduleStageWork> workRepository;
        private readonly IRepository<WorkScheduleStageWorkPeriod> periodRepository;
        private readonly IRepository<WorkScheduleStageWorkDependency> dependencyRepository;

        public SetWorkScheduleStageWorkPeriodsCommandHandler(
            IRepository<WorkScheduleStageWork> workRepository,
            IRepository<WorkScheduleStageWorkPeriod> periodRepository,
            IRepository<WorkScheduleStageWorkDependency> dependencyRepository)
        {
            this.workRepository = workRepository;
            this.periodRepository = periodRepository;
            this.dependencyRepository = dependencyRepository;
        }

        public async Task<Unit> Handle(SetWorkScheduleStageWorkPeriodsCommand request, CancellationToken cancellationToken)
        {
            WorkScheduleStageWork work = await workRepository.GetFirstBySearch(
                w => w.Id == request.WorkScheduleStageWorkId
                  && w.TenantId == request.TenantId
                  && w.ProjectId == request.ProjectId)
                ?? throw new NotFoundApiException(nameof(WorkScheduleStageWork), request.WorkScheduleStageWorkId.ToString());

            List<WorkScheduleStageWorkPeriod> newPeriods = request.Periods
                .Select(dto => new WorkScheduleStageWorkPeriod
                {
                    TenantId = request.TenantId,
                    ProjectId = request.ProjectId,
                    WorkScheduleStageWorkId = request.WorkScheduleStageWorkId,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    IsClosed = false
                })
                .ToList();

            await ValidateDependencyConstraintsAsync(request, newPeriods, cancellationToken);

            await periodRepository.ExecuteDeleteAsync(
                p => p.WorkScheduleStageWorkId == request.WorkScheduleStageWorkId,
                cancellationToken);

            work.PlannedStartDate = newPeriods.Count > 0
                ? newPeriods.Min(p => p.StartDate)
                : null;

            work.PlannedEndDate = newPeriods.Count > 0
                ? newPeriods.Max(p => p.EndDate)
                : null;

            work.UpdatedAt = DateTime.UtcNow;

            if (newPeriods.Count > 0)
            {
                await periodRepository.InsertRange(newPeriods);
            }

            await workRepository.Update(work);
            await workRepository.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }

        private async Task ValidateDependencyConstraintsAsync(
            SetWorkScheduleStageWorkPeriodsCommand request,
            List<WorkScheduleStageWorkPeriod> newPeriods,
            CancellationToken cancellationToken)
        {
            if (newPeriods.Count == 0)
                return;

            IEnumerable<WorkScheduleStageWorkDependency> dependencies = await dependencyRepository.GetBySearch(
                d => (d.PredecessorWorkId == request.WorkScheduleStageWorkId || d.SuccessorWorkId == request.WorkScheduleStageWorkId)
                  && d.TenantId == request.TenantId
                  && d.ProjectId == request.ProjectId);

            List<WorkScheduleStageWorkDependency> depList = dependencies.ToList();
            if (depList.Count == 0)
                return;

            DateTime newMinStart = newPeriods.Min(p => p.StartDate);
            DateTime newMaxEnd = newPeriods.Max(p => p.EndDate);

            HashSet<Guid> otherWorkIds = depList
                .SelectMany(d => new[] { d.PredecessorWorkId, d.SuccessorWorkId })
                .Where(id => id != request.WorkScheduleStageWorkId)
                .ToHashSet();

            IEnumerable<WorkScheduleStageWork> otherWorks = await workRepository.GetBySearch(
                w => otherWorkIds.Contains(w.Id));

            Dictionary<Guid, WorkScheduleStageWork> otherWorksById = otherWorks.ToDictionary(w => w.Id);

            foreach (WorkScheduleStageWorkDependency dep in depList)
            {
                bool thisIsPredecessor = dep.PredecessorWorkId == request.WorkScheduleStageWorkId;

                // Gdy ten zakres jest poprzednikiem, zmiana jego dat może naruszyć harmonogram następnika.
                // Następnik zostanie automatycznie przesunięty — nie blokujemy zapisu.
                if (thisIsPredecessor)
                    continue;

                if (!otherWorksById.TryGetValue(dep.PredecessorWorkId, out WorkScheduleStageWork? predecessorWork))
                    continue;

                if (predecessorWork.PlannedStartDate is null || predecessorWork.PlannedEndDate is null)
                    continue;

                DateTime predMinStart = predecessorWork.PlannedStartDate.Value;
                DateTime predMaxEnd = predecessorWork.PlannedEndDate.Value;

                int violationDays = ComputeViolationDays(predMinStart, predMaxEnd, newMinStart, newMaxEnd, dep.DependencyType, dep.LagDays);

                if (violationDays > 0)
                {
                    DateTime requiredDate = ComputeRequiredDate(predMinStart, predMaxEnd, dep.DependencyType, dep.LagDays);
                    string field = dep.DependencyType is WorkDependencyType.FinishToStart or WorkDependencyType.StartToStart
                        ? "data rozpoczęcia"
                        : "data zakończenia";
                    string lagInfo = dep.LagDays != 0
                        ? $" (przesunięcie: {dep.LagDays:+#;-#} dni)"
                        : string.Empty;

                    throw new ValidationApiException(
                        $"Zależność z \"{predecessorWork.Name}\" ({dep.DependencyType}): {field} musi być >= {requiredDate:dd.MM.yyyy}{lagInfo}");
                }
            }
        }

        private static int ComputeViolationDays(
            DateTime predMinStart,
            DateTime predMaxEnd,
            DateTime succMinStart,
            DateTime succMaxEnd,
            WorkDependencyType dependencyType,
            int lagDays)
        {
            TimeSpan delta = dependencyType switch
            {
                WorkDependencyType.FinishToStart => predMaxEnd.AddDays(lagDays) - succMinStart,
                WorkDependencyType.StartToStart => predMinStart.AddDays(lagDays) - succMinStart,
                WorkDependencyType.FinishToFinish => predMaxEnd.AddDays(lagDays) - succMaxEnd,
                WorkDependencyType.StartToFinish => predMinStart.AddDays(lagDays) - succMaxEnd,
                _ => TimeSpan.Zero
            };
            return Math.Max(0, (int)Math.Ceiling(delta.TotalDays));
        }

        private static DateTime ComputeRequiredDate(
            DateTime predMinStart,
            DateTime predMaxEnd,
            WorkDependencyType dependencyType,
            int lagDays)
        {
            return dependencyType switch
            {
                WorkDependencyType.FinishToStart => predMaxEnd.AddDays(lagDays),
                WorkDependencyType.StartToStart => predMinStart.AddDays(lagDays),
                WorkDependencyType.FinishToFinish => predMaxEnd.AddDays(lagDays),
                WorkDependencyType.StartToFinish => predMinStart.AddDays(lagDays),
                _ => DateTime.MinValue
            };
        }
    }
}
