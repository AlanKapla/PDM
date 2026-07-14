using Entities.Models.Base;

namespace Entities.Models.CostEstimates
{
    /// <summary>
    /// Schemat kolumny kosztorysu — pola podstawowe (mapowane na właściwości encji)
    /// oraz pola dodatkowe (wartości w CostEstimateAdditionalFieldValue).
    /// </summary>
    public class CostEstimateFieldSchema : BaseEntity
    {
        public Guid CostEstimateId { get; set; }

        /// <summary>
        /// Nazwa wyświetlana kolumny (edytowalna przez użytkownika).
        /// </summary>
        public string FieldName { get; set; } = default!;

        /// <summary>
        /// Techniczny identyfikator pola (np. "name", "quantity" dla pól podstawowych).
        /// </summary>
        public string FieldKey { get; set; } = default!;

        public CostEstimateFieldType FieldType { get; set; }

        public bool IsBasicField { get; set; }

        public bool IsAdditionalField { get; set; }

        public int Order { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public virtual CostEstimate CostEstimate { get; set; } = default!;

        public virtual ICollection<CostEstimateAdditionalFieldValue> Values { get; set; }
            = new List<CostEstimateAdditionalFieldValue>();
    }
}
