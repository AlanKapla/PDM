using Entities.Models.Base;
using Entities.Models.Users;

namespace Entities.Models.CostEstimates
{
    /// <summary>
    /// Plik dołączony do pola kosztorysu typu ItemSystemFiles
    /// Przechowywany w Azure Blob Storage, dozwolone formaty: PDF, JPG (max 50 MB)
    /// </summary>
    public class CostEstimateFieldFile : DeletableEntity
    {
        /// <summary>
        /// ID wartości pola (CostEstimateItemFieldValue), do której plik jest dołączony
        /// </summary>
        public Guid FieldValueId { get; set; }
        
        /// <summary>
        /// Denormalizacja — CostEstimateId z powiązanego CostEstimateItemFieldValue.
        /// Przyspiesza zapytania per kosztorys bez JOIN przez FieldValue.
        /// Musi być spójne z CostEstimateItemFieldValue.CostEstimateItem.CostEstimateId.
        /// </summary>
        public Guid CostEstimateId { get; set; }
        
        /// <summary>
        /// Oryginalna nazwa pliku przesłanego przez użytkownika
        /// </summary>
        public string OriginalFileName { get; set; } = default!;
        
        /// <summary>
        /// Nazwa bloba w Azure Blob Storage (ścieżka wewnętrzna)
        /// </summary>
        public string BlobName { get; set; } = default!;
        
        /// <summary>
        /// Typ MIME pliku (application/pdf, image/jpeg)
        /// </summary>
        public string ContentType { get; set; } = default!;
        
        /// <summary>
        /// Rozmiar pliku w bajtach
        /// </summary>
        public long FileSize { get; set; }
        
        /// <summary>
        /// Kolejność pliku w kolekcji
        /// </summary>
        public int Order { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public Guid CreatedByUserId { get; set; }
        
        // Navigation properties
        public virtual CostEstimateItemFieldValue FieldValue { get; set; } = default!;
        public virtual CostEstimate CostEstimate { get; set; } = default!;
        public virtual User CreatedByUser { get; set; } = default!;
    }
}
