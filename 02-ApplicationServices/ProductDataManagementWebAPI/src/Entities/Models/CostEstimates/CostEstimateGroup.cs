using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Entities.Models.Base;

namespace Entities.Models.CostEstimates
{
    public class CostEstimateGroup : DeletableEntity
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
        public virtual CostEstimate CostEstimate { get; set; } = default!;
        public virtual CostEstimateGroup? ParentGroup { get; set; }
        public virtual ICollection<CostEstimateGroup> ChildGroups { get; set; } = new List<CostEstimateGroup>();
        public virtual ICollection<CostEstimateGroupFieldValue> FieldValues { get; set; } = new List<CostEstimateGroupFieldValue>();
        public virtual ICollection<CostEstimateItem> Items { get; set; } = new List<CostEstimateItem>();
        public virtual ICollection<WorkScheduleStage> WorkScheduleStages { get; set; } = new List<WorkScheduleStage>();
    }
}
