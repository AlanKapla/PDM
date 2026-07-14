using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
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
        public virtual ICollection<CostEstimateItem> Items { get; set; } = new List<CostEstimateItem>();
        public virtual ICollection<WorkScheduleStage> WorkScheduleStages { get; set; } = new List<WorkScheduleStage>();

        /// <summary>
        /// Wartości pól dodatkowych dla tej grupy (nowa płaska struktura)
        /// </summary>
        public virtual ICollection<CostEstimateAdditionalFieldValue> AdditionalFieldValues { get; set; } = new List<CostEstimateAdditionalFieldValue>();
    }
}
