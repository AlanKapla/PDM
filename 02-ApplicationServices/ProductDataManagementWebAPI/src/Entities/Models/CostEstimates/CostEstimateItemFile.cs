using Entities.Models.Base;
using Entities.Models.Users;

namespace Entities.Models.CostEstimates
{
    /// <summary>
    /// Plik dołączony bezpośrednio do pozycji kosztorysu (ItemId).
    /// Zastępuje starą strukturę CostEstimateFieldFile (która była przypisana do FieldValue).
    /// Przechowywany w Azure Blob Storage, dozwolone formaty: PDF, JPG (max 50 MB)
    /// </summary>
    public class CostEstimateItemFile : DeletableEntity
    {
        /// <summary>
        /// ID pozycji kosztorysu, do której plik jest dołączony
        /// </summary>
        public Guid ItemId { get; set; }

        /// <summary>
        /// Denormalizacja — ID kosztorysu dla przyspieszenia zapytań bez JOIN
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

        /// <summary>
        /// Data utworzenia
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// ID użytkownika, który dodał plik
        /// </summary>
        public Guid CreatedByUserId { get; set; }

        // Navigation properties

        /// <summary>
        /// Pozycja kosztorysu
        /// </summary>
        public virtual CostEstimateItem Item { get; set; } = default!;

        /// <summary>
        /// Kosztorys (denormalizacja)
        /// </summary>
        public virtual CostEstimate CostEstimate { get; set; } = default!;

        /// <summary>
        /// Użytkownik, który dodał plik
        /// </summary>
        public virtual User CreatedByUser { get; set; } = default!;
    }
}
