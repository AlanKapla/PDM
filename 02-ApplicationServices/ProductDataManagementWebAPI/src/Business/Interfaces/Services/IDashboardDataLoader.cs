using Entities.Models.CostEstimates;
using Entities.Models.Costs;
using Entities.Models.CostTrackers;
using Entities.Models.Projects;
using Entities.Models.WorkSchedules;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Surowe dane dashboardu projektu pobrane z bazy w jednym przebiegu.
    /// </summary>
    public sealed record DashboardData(
        List<BaseCost> AllCosts,
        ILookup<Guid, BaseCostAttachment> AttachmentsByCostId,
        Dictionary<Guid, (Guid? CostEstimateId, Guid? CostEstimateItemId)> CostEstimateContext,
        List<CostEstimate> AllEstimates,
        ProjectCurrency? ProjectCurrency,
        List<WorkSchedule> AllSchedules,
        List<WorkScheduleStage> AllStages,
        List<WorkScheduleStageWork> AllStageWorks,
        HashSet<Guid> ClosedWorkIds,
        Dictionary<Guid, CostEstimateItem> StageWorkLinkedItems
    );

    /// <summary>
    /// Pobiera surowe dane potrzebne do zbudowania ProjectDashboardWeb.
    /// </summary>
    public interface IDashboardDataLoader
    {
        Task<DashboardData> LoadAsync(Guid tenantId, Guid projectId, CancellationToken cancellationToken);
    }
}
