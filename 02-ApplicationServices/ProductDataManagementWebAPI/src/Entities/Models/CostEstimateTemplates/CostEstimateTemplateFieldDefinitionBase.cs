using Entities.Models.Base;
using Entities.Models.CostEstimates;

namespace Entities.Models.CostEstimateTemplates
{
    /// <summary>
    /// Bazowa klasa dla wszystkich definicji pól w szablonie kosztorysu
    /// Unified model - wszystkie pola (Group, ItemSystem, ItemCalculated, ItemGeneric) dziedziczą z tej klasy
    /// </summary>
    public abstract class CostEstimateTemplateFieldDefinitionBase : BaseEntity
    {
        /// <summary>
        /// ID wersji szablonu do której należy pole
        /// </summary>
        public Guid TemplateVersionId { get; set; }
        
        /// <summary>
        /// Nazwa pola (UI-defined identifier) - Guid generowany przez frontend
        /// Służy do identyfikacji pola w wartościach (CostEstimateItemFieldValue.FieldName)
        /// </summary>
        public Guid FieldName { get; set; }
        
        /// <summary>
        /// Zakres pola - określa do czego pole należy
        /// Group, ItemSystem, ItemCalculated, ItemGeneric
        /// </summary>
        public FieldScope FieldScope { get; set; }
        
        /// <summary>
        /// Typ pola z unified enum (z prefiksami)
        /// GroupName, ItemSystemName, ItemCalculatedUnitPriceNet, ItemGenericString, etc.
        /// </summary>
        public FieldType FieldType { get; set; }
        
        /// <summary>
        /// Etykieta wyświetlana w UI
        /// </summary>
        public string Label { get; set; } = default!;
        
        /// <summary>
        /// Czy pole może być sortowane w tabeli
        /// </summary>
        public bool IsSortable { get; set; }
        
        /// <summary>
        /// Czy pole może być filtrowane
        /// </summary>
        public bool IsFilterable { get; set; }
        
        /// <summary>
        /// ID pola nadrzędnego (dla pól zagnieżdżonych w opcjach)
        /// Tylko pola typu ItemSystemOptions mogą mieć pola potomne
        /// </summary>
        public Guid? ParentFieldId { get; set; }
        
        /// <summary>
        /// Kolejność wyświetlania w UI (dla pól nadrzędnych, gdzie ParentFieldId == null)
        /// </summary>
        public int Order { get; set; }
        
        // Navigation properties
        
        /// <summary>
        /// Wersja szablonu do której należy pole
        /// </summary>
        public virtual CostEstimateTemplateVersion TemplateVersion { get; set; } = default!;
        
        /// <summary>
        /// Pole nadrzędne (dla pól zagnieżdżonych w opcjach)
        /// </summary>
        public virtual CostEstimateTemplateFieldDefinitionBase? ParentField { get; set; }
        
        /// <summary>
        /// Pola potomne (dla pól typu ItemSystemOptions)
        /// Tylko FieldType = ItemSystemOptions może mieć niepustą kolekcję
        /// </summary>
        public virtual ICollection<CostEstimateTemplateFieldDefinitionBase> ChildFields { get; set; } = new List<CostEstimateTemplateFieldDefinitionBase>();
    }
}
