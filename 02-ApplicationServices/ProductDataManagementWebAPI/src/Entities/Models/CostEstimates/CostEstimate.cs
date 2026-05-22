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
using Entities.Models.CostEstimateTemplates;
using Entities.Models.CostTrackers;

namespace Entities.Models.CostEstimates
{
    public class CostEstimate : DeletableEntity
    {
        public Guid TenantId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid TemplateId { get; set; }
        public Guid OwnerId { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public CostEstimateStatus Status { get; set; }
        public decimal? TotalNet { get; set; }
        public decimal? TotalGross { get; set; }
        public decimal? TotalVat { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastCalculatedAt { get; set; }
        public virtual Tenant Tenant { get; set; } = default!;
        public virtual Project Project { get; set; } = default!;
        public virtual CostEstimateTemplate Template { get; set; } = default!;
        public virtual User Owner { get; set; } = default!;
        public virtual ICollection<CostEstimateGroup> AllGroups { get; set; } = new List<CostEstimateGroup>();
        public virtual ICollection<CostEstimateItem> AllItems { get; set; } = new List<CostEstimateItem>();
        public virtual ICollection<WorkSchedule> WorkSchedules { get; set; } = new List<WorkSchedule>();
        public IEnumerable<CostEstimateGroup> RootGroups => AllGroups?.Where(g => g.ParentGroupId == null) ?? Enumerable.Empty<CostEstimateGroup>();
        
        /// <summary>
        /// Populate Options and Components dla wszystkich pozycji z AllItems
        /// Wywołaj PO załadowaniu z bazy (po Include AllItems)
        /// </summary>
        public void PopulateItemHierarchy()
        {
            if (AllItems == null) return;
            
            // Grupuj child items po ParentItemId
            var childItemsByParent = AllItems
                .Where(i => i.ParentItemId.HasValue)
                .GroupBy(i => i.ParentItemId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());
            
            // Dla każdej głównej pozycji, ustaw jej child items
            foreach (var item in AllItems.Where(i => !i.ParentItemId.HasValue))
            {
                if (childItemsByParent.TryGetValue(item.Id, out var childItems))
                {
                    item.SetChildItems(childItems);
                }
            }
            
            // Dla każdego child item (który może mieć swoje Options), ustaw jego child items
            foreach (var item in AllItems.Where(i => i.ParentItemId.HasValue))
            {
                if (childItemsByParent.TryGetValue(item.Id, out var childItems))
                {
                    item.SetChildItems(childItems);
                }
            }
        }
    }
}
