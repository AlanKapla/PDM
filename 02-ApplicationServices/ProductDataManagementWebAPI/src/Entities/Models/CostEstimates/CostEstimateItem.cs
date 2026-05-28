using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Entities.Models.Base;
using Entities.Models.CostTrackers;

namespace Entities.Models.CostEstimates
{
    /// <summary>
    /// Pozycja kosztorysu (work scope item).
    /// Może mieć kolekcję opcji (Options) jeśli w FieldValues istnieje pole typu ItemSystemOptions.
    /// Może mieć kolekcję komponentów (Components) - wtedy NIE MOŻE mieć FieldValues.
    /// </summary>
    public class CostEstimateItem : DeletableEntity
    {
        public Guid CostEstimateId { get; set; }
        public string Name { get; set; } = default!;
        public Guid GroupId { get; set; }
        
        /// <summary>
        /// ID pozycji nadrzędnej (parent item)
        /// NULL gdy pozycja jest główna (RelationType = None)
        /// Wypełnione gdy pozycja jest opcją (RelationType = Option) lub komponentem (RelationType = Component)
        /// </summary>
        public Guid? ParentItemId { get; set; }
        
        /// <summary>
        /// Typ relacji do pozycji nadrzędnej
        /// None = pozycja główna, Option = opcja, Component = komponent
        /// </summary>
        public ItemRelationType RelationType { get; set; }
        
        public int Order { get; set; }
        
        /// <summary>
        /// Wartość netto pozycji (obliczana)
        /// Jeśli pozycja ma Components - suma z Components, jeśli nie - z FieldValues
        /// </summary>
        public decimal? NetValue { get; set; }
        
        /// <summary>
        /// Wartość brutto pozycji (obliczana)
        /// Jeśli pozycja ma Components - suma z Components, jeśli nie - z FieldValues
        /// </summary>
        public decimal? GrossValue { get; set; }
        
        /// <summary>
        /// Wartość VAT pozycji (obliczana)
        /// Jeśli pozycja ma Components - suma z Components, jeśli nie - z FieldValues
        /// </summary>
        public decimal? VatValue { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        // Navigation properties
        public virtual CostEstimate CostEstimate { get; set; } = default!;
        public virtual CostEstimateGroup Group { get; set; } = default!;
        public virtual ICollection<CostEstimateItemFieldValue> FieldValues { get; set; } = new List<CostEstimateItemFieldValue>();
        
        /// <summary>
        /// Pozycja nadrzędna (jeśli ta pozycja jest opcją lub komponentem)
        /// </summary>
        public virtual CostEstimateItem? ParentItem { get; set; }
        public virtual ICollection<TrackedCost>? TrackedCosts { get; set; }
        
        /// <summary>
        /// Kolekcja child items (Options + Components razem)
        /// EF nie rozróżnia - musisz filtrować po RelationType w kodzie
        /// </summary>
        private ICollection<CostEstimateItem>? _childItems;
        
        /// <summary>
        /// Kolekcja opcji (zagnieżdżonych pozycji)
        /// Wypełniona tylko gdy pozycja/komponent ma pole ItemSystemOptions w FieldValues
        /// Filtrowane z AllItems (załadowanych przez Include) gdzie ParentItemId == this.Id && RelationType = Option
        /// </summary>
        public ICollection<CostEstimateItem> Options
        {
            get
            {
                // Jeśli _childItems jest null, zwróć pustą listę
                // W praktyce _childItems będzie wypełnione przez EF gdy załadujesz AllItems z filtrem ParentItemId
                if (_childItems == null) return new List<CostEstimateItem>();
                
                return _childItems.Where(c => c.RelationType == ItemRelationType.Option).ToList();
            }
        }
        
        /// <summary>
        /// Kolekcja komponentów (składników pozycji)
        /// Wypełniona gdy pozycja główna składa się z komponentów (robocizna, materiał, etc.)
        /// WAŻNE: Pozycja z komponentami NIE MOŻE mieć FieldValues!
        /// Filtrowane z AllItems (załadowanych przez Include) gdzie ParentItemId == this.Id && RelationType = Component
        /// </summary>
        public ICollection<CostEstimateItem> Components
        {
            get
            {
                // Jeśli _childItems jest null, zwróć pustą listę
                if (_childItems == null) return new List<CostEstimateItem>();
                
                return _childItems.Where(c => c.RelationType == ItemRelationType.Component).ToList();
            }
        }
        
        /// <summary>
        /// Helper method - ustawia child items (używane przez EF lub kod ładujący)
        /// </summary>
        public void SetChildItems(IEnumerable<CostEstimateItem> childItems)
        {
            _childItems = childItems.ToList();
        }
    }
}
