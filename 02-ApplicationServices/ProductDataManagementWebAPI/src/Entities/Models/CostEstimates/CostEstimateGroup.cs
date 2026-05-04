using Entities.Models;
using Entities.Models.Base;
using Entities.Models.WorkItemLinks;

namespace Entities.Models.CostEstimates
{
    public class CostEstimateGroup : BaseEntity
    {
        public Guid CostEstimateId { get; set; }
        public string Name { get; set; } = default!;
        public Guid? ParentGroupId { get; set; }
        public int Level { get; set; }
        public int Order { get; set; }
        public decimal? TotalNet { get; set; }
        public decimal? TotalGross { get; set; }
        public decimal? TotalVat { get; set; }
        public DateTime? LastCalculatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        
        public virtual CostEstimate CostEstimate { get; set; } = default!;
        public virtual CostEstimateGroup? ParentGroup { get; set; }
        public virtual ICollection<CostEstimateGroup> ChildGroups { get; set; } = new List<CostEstimateGroup>();
        public virtual ICollection<CostEstimateGroupFieldValue> FieldValues { get; set; } = new List<CostEstimateGroupFieldValue>();
        public virtual ICollection<CostEstimateItem> Items { get; set; } = new List<CostEstimateItem>();
        public virtual ICollection<CostEstimateGroupWorkScheduleStageLink> WorkScheduleStageLinks { get; set; } = new List<CostEstimateGroupWorkScheduleStageLink>();
    }
}
