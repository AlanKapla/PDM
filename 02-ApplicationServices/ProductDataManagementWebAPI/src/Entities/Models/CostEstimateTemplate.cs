using Entities.Models.Base;
using Entities.Models.CostEstimateTemplateDefinitions;

namespace Entities.Models
{
    /// <summary>
    /// Szablon kosztorysu - definicja struktury kosztorysu wielokrotnego użytku
    /// </summary>
    public class CostEstimateTemplate : BaseEntity
    {
        /// <summary>
        /// ID właściciela szablonu (User)
        /// </summary>
        public Guid OwnerId { get; set; }
        
        /// <summary>
        /// Nazwa szablonu
        /// </summary>
        public string Name { get; set; } = default!;
        
        /// <summary>
        /// Opis szablonu
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// Definicja struktury szablonu
        /// Przechowywana jako JSON column w bazie danych
        /// </summary>
        public CostEstimateTemplateStructure TemplateStructure { get; set; } = default!;
        
        /// <summary>
        /// Data utworzenia
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Data ostatniej aktualizacji
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
        
        /// <summary>
        /// Soft delete
        /// </summary>
        public bool IsDeleted { get; set; }
        
        /// <summary>
        /// Data usunięcia
        /// </summary>
        public DateTime? DeletedAt { get; set; }
        
        // Navigation properties
        
        /// <summary>
        /// Właściciel szablonu
        /// </summary>
        public virtual User Owner { get; set; } = default!;
    }
}
