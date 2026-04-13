using Entities.Models.Base;
using Entities.Models.CostEstimates;

namespace Entities.Models.CostTrackers
{
    public class TrackedCost : BaseEntity
    {
        public Guid TrackerId { get; set; }
        public Guid? CostEstimateId { get; set; }
        public Guid? CostEstimateItemId { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public decimal? Net { get; set; }
        public decimal? Gross { get; set; }
        public string? Contractor { get; set; }
        public DateTime? Date { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }

        public virtual CostTracker Tracker { get; set; } = default!;
        public virtual CostEstimateItem? CostEstimateItem { get; set; }
        public virtual ICollection<TrackedCostAttachment> Attachments { get; set; } = new List<TrackedCostAttachment>();
        public virtual ProjectCostTrackedCostLink? ProjectCostLink { get; set; }
    }
}
