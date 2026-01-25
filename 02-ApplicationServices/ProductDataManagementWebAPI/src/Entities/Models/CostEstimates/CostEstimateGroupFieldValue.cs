using Entities.Models.Base;
using Entities.Models.CostEstimateTemplates;

namespace Entities.Models.CostEstimates
{
    /// <summary>
    /// Wypełniona wartość pola nagłówka grupy
    /// </summary>
    public class CostEstimateGroupFieldValue : BaseEntity
    {
        /// <summary>
        /// ID grupy
        /// </summary>
        public Guid GroupId { get; set; }
        
        /// <summary>
        /// ID definicji pola z szablonu
        /// </summary>
        public Guid FieldDefinitionId { get; set; }
        
        /// <summary>
        /// Wartość pola jako string (parsowana na podstawie typu z definicji)
        /// </summary>
        public string? Value { get; set; }
        
        /// <summary>
        /// Data utworzenia wartości
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Data ostatniej aktualizacji
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        
        /// <summary>
        /// Grupa
        /// </summary>
        public virtual CostEstimateGroup Group { get; set; } = default!;
        
        /// <summary>
        /// Definicja pola nagłówka grupy z szablonu
        /// </summary>
        public virtual CostEstimateTemplateGroupFieldDefinition FieldDefinition { get; set; } = default!;
    }
}
