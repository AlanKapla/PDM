using Entities.Models.CostEstimates;
using Entities.Models.WorkItemLinks;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Serwis zarządzający cyklem życia łączników CostEstimateItemWorkScheduleStageWorkLink,
    /// CostEstimateGroupWorkScheduleStageLink i CostEstimateWorkScheduleLink.
    /// Jedyne miejsce tworzenia łączników to WorkScheduleSyncService — handlery nie tworzą łączników samodzielnie.
    /// </summary>
    public interface IWorkItemLinkService
    {
        // ── Odczyt ──────────────────────────────────────────────────────────────

        Task<CostEstimateWorkScheduleLink?> GetWorkScheduleLinkAsync(
            Guid workScheduleId,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<CostEstimateGroupWorkScheduleStageLink>> GetGroupStageLinksForWorkScheduleLinkAsync(
            Guid workScheduleLinkId,
            CancellationToken cancellationToken);

        // ── Tworzenie (wywoływane wyłącznie przez CreateWorkScheduleCommandHandler i WorkScheduleSyncService) ──

        Task<CostEstimateWorkScheduleLink> CreateWorkScheduleLinkAsync(
            Guid workScheduleId,
            Guid? costEstimateId,
            CancellationToken cancellationToken);

        Task CreateGroupStageLinkForScheduleStageAsync(
            Guid workScheduleId,
            Guid stageId,
            Guid? costEstimateGroupId,
            CancellationToken cancellationToken);

        // ── Usuwanie pozycji (item link only) ───────────────────────────────────

        /// <summary>
        /// Usuwa CostEstimateItemWorkScheduleStageWorkLink powiązany z danym zakresem pracy.
        /// </summary>
        Task DeleteWorkItemLinkForWorkAsync(
            Guid workScheduleStageWorkId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Usuwa CostEstimateItemWorkScheduleStageWorkLinki dla wielu zakresów pracy (bulk).
        /// </summary>
        Task DeleteWorkItemLinksForWorksAsync(
            IReadOnlyCollection<Guid> workIds,
            CancellationToken cancellationToken);

        /// <summary>
        /// Usuwa CostEstimateItemWorkScheduleStageWorkLinki powiązane z podanymi pozycjami kosztorysu.
        /// </summary>
        Task DeleteWorkItemLinksForItemsAsync(
            IReadOnlyCollection<Guid> costEstimateItemIds,
            CancellationToken cancellationToken);

        // ── Usuwanie etapu/grupy (group-stage link + item links) ─────────────────

        /// <summary>
        /// Usuwa CostEstimateGroupWorkScheduleStageLinki dla podanych etapów harmonogramu.
        /// Item linki powiązane z pracami tych etapów należy usunąć osobno przez DeleteWorkItemLinksForWorksAsync.
        /// </summary>
        Task DeleteGroupStageLinksForStagesAsync(
            IReadOnlyCollection<Guid> stageIds,
            CancellationToken cancellationToken);

        /// <summary>
        /// Usuwa CostEstimateGroupWorkScheduleStageLinki dla podanych grup kosztorysu.
        /// Item linki powiązane z pozycjami tych grup należy usunąć osobno przez DeleteWorkItemLinksForItemsAsync.
        /// </summary>
        Task DeleteGroupStageLinksForGroupsAsync(
            IReadOnlyCollection<Guid> costEstimateGroupIds,
            CancellationToken cancellationToken);

        // ── Usuwanie całego obiektu (wszystkie łączniki) ─────────────────────────

        /// <summary>
        /// Usuwa wszystkie łączniki powiązane z harmonogramem:
        /// item linki → group-stage linki → work schedule link.
        /// </summary>
        Task DeleteAllLinksForScheduleAsync(
            Guid workScheduleId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Usuwa wszystkie łączniki powiązane z kosztorysem:
        /// item linki → group-stage linki → work schedule link.
        /// </summary>
        Task DeleteAllLinksForEstimateAsync(
            Guid costEstimateId,
            CancellationToken cancellationToken);

        // ── Synchronizacja danych denormalizowanych ──────────────────────────────

        Task SyncWorkItemLinkAsync(
            Guid? workItemLinkId,
            Guid? costEstimateItemId,
            Guid? workScheduleStageWorkId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Tworzy lub aktualizuje CostEstimateItemWorkScheduleStageWorkLink dla danego zakresu pracy.
        /// Wywoływany przez WorkScheduleSyncService przy każdym upsert WorkScheduleStageWork.
        /// </summary>
        Task UpsertWorkItemLinkAsync(
            Guid projectId,
            Guid groupStageLinkId,
            Guid costEstimateItemId,
            Guid workScheduleStageWorkId,
            string displayName,
            decimal? budgetNet,
            decimal? budgetGross,
            int order,
            CancellationToken cancellationToken);

        Task SyncPlannedDatesForStageWorkAsync(
            Guid workScheduleStageWorkId,
            DateTime? plannedStart,
            DateTime? plannedEnd,
            bool isWorkClosed,
            CancellationToken cancellationToken);
    }
}
