using Entities.Models.Base;

namespace Entities.Models.CostEstimates
{
    /// <summary>
    /// Wartość pola dodatkowego — przypisana do grupy (GroupId) LUB pozycji (ItemId).
    /// Tylko jedna z wartości (StringValue/DecimalValue/BoolValue/DateTimeValue) jest wypełniona,
    /// zgodnie z AdditionalFieldType definicji pola nadrzędnego.
    /// </summary>
    public class CostEstimateAdditionalFieldValue : BaseEntity
    {
        /// <summary>
        /// ID wpisu schematu pola (dla pól dodatkowych).
        /// </summary>
        public Guid FieldSchemaId { get; set; }

        /// <summary>
        /// ID grupy, do której należy wartość (nullable — dla wartości na poziomie grupy)
        /// </summary>
        public Guid? GroupId { get; set; }

        /// <summary>
        /// ID pozycji, do której należy wartość (nullable — dla wartości na poziomie pozycji)
        /// </summary>
        public Guid? ItemId { get; set; }

        /// <summary>
        /// Wartość tekstowa (dla AdditionalFieldType.String)
        /// </summary>
        public string? StringValue { get; set; }

        /// <summary>
        /// Wartość liczbowa (dla AdditionalFieldType.Decimal)
        /// </summary>
        public decimal? DecimalValue { get; set; }

        /// <summary>
        /// Wartość logiczna (dla AdditionalFieldType.Boolean)
        /// </summary>
        public bool? BoolValue { get; set; }

        /// <summary>
        /// Wartość daty/czasu (dla AdditionalFieldType.DateTime)
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

        // Navigation properties

        /// <summary>
        /// Definicja pola w schemacie kosztorysu
        /// </summary>
        public virtual CostEstimateFieldSchema FieldSchema { get; set; } = default!;

        /// <summary>
        /// Grupa (jeśli wartość jest przypisana do grupy)
        /// </summary>
        public virtual CostEstimateGroup? Group { get; set; }

        /// <summary>
        /// Pozycja (jeśli wartość jest przypisana do pozycji)
        /// </summary>
        public virtual CostEstimateItem? Item { get; set; }
    }
}
