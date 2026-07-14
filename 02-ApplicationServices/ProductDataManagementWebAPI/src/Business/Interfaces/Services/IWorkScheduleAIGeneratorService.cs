using Business.Interfaces.WebModels.WorkSchedules;

namespace Business.Interfaces.Services
{
    /// <summary>
    /// Generates work schedule durations and dependencies using AI analysis of cost estimate structure.
    /// </summary>
    public interface IWorkScheduleAIGeneratorService
    {
        /// <summary>
        /// Analyzes stage and work names from a cost estimate and generates suggested durations
        /// and dependencies within the given overall time frame.
        /// </summary>
        /// <param name="workScheduleId">The work schedule ID (already synced with cost estimate).</param>
        /// <param name="tenantId">Tenant ID.</param>
        /// <param name="projectId">Project ID.</param>
        /// <param name="stages">List of stages with names and hierarchy.</param>
        /// <param name="works">List of work items with names, stage assignments and ordering.</param>
        /// <param name="overallStartDate">Overall project start date.</param>
        /// <param name="overallEndDate">Overall project end date.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>AI-generated schedule result with periods and dependencies.</returns>
        Task<AIScheduleResult> GenerateScheduleAsync(
            Guid workScheduleId,
            Guid tenantId,
            Guid projectId,
            List<StageInput> stages,
            List<WorkInput> works,
            DateTime overallStartDate,
            DateTime overallEndDate,
            CancellationToken cancellationToken);
    }

    public sealed record StageInput
    {
        public Guid Id { get; init; }
        public Guid? ParentStageId { get; init; }
        public string Name { get; init; } = string.Empty;
        public int Order { get; init; }
    }

    public sealed record WorkInput
    {
        public Guid Id { get; init; }
        public Guid StageId { get; init; }
        public string Name { get; init; } = string.Empty;
        public int Order { get; init; }
        public string StageName { get; init; } = string.Empty;
    }
}
