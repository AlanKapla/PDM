using Entities.Models.CostEstimates;

namespace Entities.Models.CostEstimateTemplates
{
    /// <summary>
    /// Definicja pola systemowego pozycji kosztorysu (Item)
    /// Używa FieldScope = ItemSystem i FieldType z prefiksem ItemSystem (np. FieldType.ItemSystemName)
    /// </summary>
    public class CostEstimateTemplateItemSystemFieldDefinition : CostEstimateTemplateFieldDefinitionBase
    {

    }
    
    /// <summary>
    /// Definicja pola obliczeniowego pozycji kosztorysu (Item)
    /// Używa FieldScope = ItemCalculated i FieldType z prefiksem ItemCalculated (np. FieldType.ItemCalculatedUnitPriceNet)
    /// </summary>
    public class CostEstimateTemplateItemCalculatedFieldDefinition : CostEstimateTemplateFieldDefinitionBase
    {
        /// <summary>
        /// Czy pole jest sumowane w podsumowaniu grup (dla pól nadrzędnych, gdzie ParentFieldId == null)
        /// </summary>
        public bool SumInGroup { get; set; }
        
        /// <summary>
        /// Czy pole jest sumowane w podsumowaniu całkowitym (dla pól nadrzędnych, gdzie ParentFieldId == null)
        /// </summary>
        public bool SumInTotal { get; set; }
    }
    
    /// <summary>
    /// Definicja pola generycznego pozycji kosztorysu (Item)
    /// Używa FieldScope = ItemGeneric i FieldType z prefiksem ItemGeneric (np. FieldType.ItemGenericString)
    /// </summary>
    public class CostEstimateTemplateItemGenericFieldDefinition : CostEstimateTemplateFieldDefinitionBase
    {

    }
}
