using Entities.Models.Base;
using Entities.Models.CostEstimateTemplates;

namespace Entities.Models.CostEstimates
{
    /// <summary>
    /// Wartość pola w pozycji kosztorysu.
    /// Używa pojedynczego FieldDefinitionId wskazującego na CostEstimateTemplateFieldDefinitionBase.
    /// Typ pola określamy przez FieldDefinition.FieldScope:
    /// - FieldScope.ItemSystem -> CostEstimateTemplateItemSystemFieldDefinition
    /// - FieldScope.ItemCalculated -> CostEstimateTemplateItemCalculatedFieldDefinition
    /// - FieldScope.ItemGeneric -> CostEstimateTemplateItemGenericFieldDefinition
    /// 
    /// EF Core używa Table-Per-Hierarchy (TPH) więc wszystkie typy są w jednej tabeli
    /// i polimorfizm jest obsługiwany automatycznie.
    /// 
    /// Wartość zapisywana w odpowiednim polu typowanym (StringValue/DecimalValue/BoolValue/DateTimeValue) 
    /// w zależności od FieldType definicji pola
    /// </summary>
    public class CostEstimateItemFieldValue : CostEstimateFieldValueBase
    {
        public Guid ItemId { get; set; }
        
        /// <summary>
        /// ID definicji pola - wskazuje na CostEstimateTemplateFieldDefinitionBase
        /// Konkretny typ (System/Calculated/Generic) określany przez FieldDefinition.FieldScope
        /// </summary>
        public Guid FieldDefinitionId { get; set; }
        
        // Navigation properties
        public virtual CostEstimateItem Item { get; set; } = default!;
        
        /// <summary>
        /// Nawigacja do definicji pola - może być dowolnym typem dziedziczącym po CostEstimateTemplateFieldDefinitionBase
        /// Użyj pattern matching lub FieldScope aby określić konkretny typ:
        /// - if (FieldDefinition is CostEstimateTemplateItemSystemFieldDefinition systemField) { ... }
        /// - if (FieldDefinition.FieldScope == FieldScope.ItemCalculated) { ... }
        /// </summary>
        public virtual CostEstimateTemplateFieldDefinitionBase FieldDefinition { get; set; } = default!;

        /// <summary>
        /// Kolekcja plików dołączonych do tego pola (tylko dla FieldType == ItemSystemFiles)
        /// </summary>
        public virtual ICollection<CostEstimateFieldFile> Files { get; set; } = new List<CostEstimateFieldFile>();
    }
}
