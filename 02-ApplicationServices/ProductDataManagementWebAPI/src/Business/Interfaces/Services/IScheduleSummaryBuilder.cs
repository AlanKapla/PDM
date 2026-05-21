using Business.Interfaces.WebModels.CostTrackers;
using Entities.Models.WorkSchedules;
using Entities.Models.CostEstimates;
using Entities.Models.Costs;
using Entities.Models.CostTrackers;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Buduje ScheduleSummaryWeb z surowych danych harmonogramu.
    /// </summary>
    public interface IScheduleSummaryBuilder
    {
        List<ScheduleSummaryWeb> BuildAll(
            List<WorkSchedule> schedules,
            List<WorkScheduleStage> allStages,
            List<WorkScheduleStageWork> allStageWorks,
            HashSet<Guid> closedWorkIds,
            Dictionary<Guid, CostEstimateItem> stageWorkLinkedItems,
            List<BaseCost> allCosts,
            ILookup<Guid, BaseCostAttachment> attachmentsByCostId,
            DateTime referenceDate,
            Dictionary<Guid, string> contractorNames);
    }
}
