using Entities.Models.Base;
using Entities.Models.CostEstimates;
using Entities.Models.WorkItemLinks;

namespace Entities.Models.CostTrackers
{
    public class TrackedCost : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid? WorkItemLinkId { get; set; }

        /// <summary>
        /// ID pozycji kosztorysu.
        /// Wypełnione tylko gdy koszt jest przypisany bezpośrednio do pozycji kosztorysu
        /// BEZ łącznika (WorkItemLinkId == null).
        /// </summary>
        public Guid? CostEstimateItemId { get; set; }

        /// <summary>
        /// ID zakresu pracy harmonogramu.
        /// Wypełnione tylko gdy koszt jest przypisany bezpośrednio do zakresu pracy
        /// BEZ łącznika (WorkItemLinkId == null).
        /// </summary>
        public Guid? WorkScheduleStageWorkId { get; set; }

        public string Name { get; set; } = default!;
        public string? Number { get; set; }
        public string? Description { get; set; }
        public decimal? Net { get; set; }
        public decimal? Gross { get; set; }
        public string? Contractor { get; set; }
        public DateTime? Date { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual CostEstimateItemWorkScheduleStageWorkLink? CostEstimateItemWorkScheduleStageWorkLink { get; set; }
        public virtual CostEstimateItem? CostEstimateItem { get; set; }
        public virtual WorkScheduleStageWork? WorkScheduleStageWork { get; set; }
        public virtual ICollection<TrackedCostAttachment> Attachments { get; set; } = new List<TrackedCostAttachment>();

        public void ValidateLinkExclusivity()
        {
            bool hasLink = WorkItemLinkId.HasValue;
            bool hasDirectBinding = CostEstimateItemId.HasValue || WorkScheduleStageWorkId.HasValue;

            if (hasLink && hasDirectBinding)
            {
                throw new InvalidOperationException(
                    "TrackedCost cannot have WorkItemLinkId set together with CostEstimateItemId or WorkScheduleStageWorkId.");
            }
        }
    }
}
