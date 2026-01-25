using Entities.Models.Base;

namespace Entities.Models.CostEstimates
{
    /// <summary>
    /// Pozycja kosztorysu (work scope item).
    /// Może mieć kolekcję opcji (zagnieżdżone pozycje) jeśli w FieldValues
    /// istnieje pole typu ItemSystemOptions.
    /// </summary>
    public class CostEstimateItem : BaseEntity
    {
        public Guid CostEstimateId { get; set; }
        public Guid GroupId { get; set; }
        
        /// <summary>
        /// ID pozycji nadrzędnej (parent item) - wypełnione gdy ta pozycja jest opcją
        /// NULL gdy pozycja jest główna (nie jest opcją)
        /// </summary>
        public Guid? ParentItemId { get; set; }
        
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        
        // Navigation properties
        public virtual CostEstimate CostEstimate { get; set; } = default!;
        public virtual CostEstimateGroup Group { get; set; } = default!;
        public virtual ICollection<CostEstimateItemFieldValue> FieldValues { get; set; } = new List<CostEstimateItemFieldValue>();
        
        /// <summary>
        /// Pozycja nadrzędna (jeśli ta pozycja jest opcją)
        /// </summary>
        public virtual CostEstimateItem? ParentItem { get; set; }
        
        /// <summary>
        /// Kolekcja opcji (zagnieżdżonych pozycji)
        /// Wypełniona tylko gdy pozycja ma pole ItemSystemOptions w FieldValues
        /// </summary>
        public virtual ICollection<CostEstimateItem> Options { get; set; } = new List<CostEstimateItem>();
    }
}
