using Entities.Models.Base;

namespace Entities.Models.CostEstimates
{
    /// <summary>
    /// Bazowa klasa dla wartości pól w kosztorysie
    /// Używa typowanych właściwości zamiast pojedynczego stringa
    /// Wartość jest zapisywana w odpowiednim polu w zależności od FieldType
    /// </summary>
    public abstract class CostEstimateFieldValueBase : BaseEntity
    {
        /// <summary>
        /// Wartość tekstowa (dla pól typu String, Text)
        /// Używane dla FieldType: GroupName, GroupDescription, GroupNumber, GroupStatus, GroupNotes, GroupResponsible, ItemSystemName, ItemSystemUnit, ItemGenericString
        /// </summary>
        public string? StringValue { get; set; }
        
        /// <summary>
        /// Wartość liczbowa (dla pól typu Decimal, Number)
        /// Używane dla FieldType: GroupBudget, ItemSystemQuantity, ItemCalculatedUnitPriceNet, ItemCalculatedVatRate, ItemCalculatedUnitPriceGross, 
        /// ItemCalculatedValueNet, ItemCalculatedValueGross, ItemCalculatedUnitVat, ItemCalculatedTotalVat, ItemGenericNumber
        /// </summary>
        public decimal? DecimalValue { get; set; }
        
        /// <summary>
        /// Wartość logiczna (dla pól typu Boolean)
        /// Używane dla FieldType: ItemSystemSelected, ItemGenericBoolean
        /// </summary>
        public bool? BoolValue { get; set; }
        
        /// <summary>
        /// Wartość daty/czasu (dla pól typu Date, DateTime)
        /// Używane dla FieldType: GroupStartDate, GroupEndDate, ItemGenericDate, ItemGenericDateTime
        /// </summary>
        public DateTime? DateTimeValue { get; set; }
        
        /// <summary>
        /// Data utworzenia wartości
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Data ostatniej aktualizacji
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
