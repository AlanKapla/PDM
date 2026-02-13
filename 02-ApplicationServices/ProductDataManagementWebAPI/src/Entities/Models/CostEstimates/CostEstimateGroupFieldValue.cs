using Entities.Models.Base;
using Entities.Models.CostEstimateTemplates;

namespace Entities.Models.CostEstimates
{
    /// <summary>
    /// Wypełniona wartość pola nagłówka grupy
    /// Wartość zapisywana w odpowiednim polu typowanym (StringValue/DecimalValue/BoolValue/DateTimeValue) 
    /// w zależności od FieldType definicji pola
    /// </summary>
    public class CostEstimateGroupFieldValue : CostEstimateFieldValueBase
    {
        /// <summary>
        /// ID grupy
        /// </summary>
        public Guid GroupId { get; set; }
        
        /// <summary>
        /// ID definicji pola z szablonu
        /// </summary>
        public Guid FieldDefinitionId { get; set; }
        
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
