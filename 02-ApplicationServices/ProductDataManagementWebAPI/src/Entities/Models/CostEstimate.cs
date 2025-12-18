using Entities.Models.Base;
using Entities.Models.CostEstimateData;

namespace Entities.Models
{
    /// <summary>
    /// Wypełniony kosztorys na podstawie szablonu
    /// </summary>
    public class CostEstimate : BaseEntity
    {
        /// <summary>
        /// ID tenant (multi-tenancy)
        /// </summary>
        public Guid TenantId { get; set; }
        
        /// <summary>
        /// ID projektu
        /// </summary>
        public Guid ProjectId { get; set; }
        
        /// <summary>
        /// ID szablonu kosztorysu (struktura/schemat)
        /// </summary>
        public Guid TemplateId { get; set; }
        
        /// <summary>
        /// ID właściciela kosztorysu (User)
        /// </summary>
        public Guid OwnerId { get; set; }
        
        /// <summary>
        /// Nazwa kosztorysu
        /// </summary>
        public string Name { get; set; } = default!;
        
        /// <summary>
        /// Opis kosztorysu
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// Status kosztorysu
        /// </summary>
        public CostEstimateStatus Status { get; set; }
        
        /// <summary>
        /// Wypełnione dane kosztorysu
        /// Struktura zgodna z CostEstimateTemplate.TemplateStructure
        /// Przechowuje hierarchię grup, zakresy robót z wartościami pól
        /// Serializowane do JSON w bazie danych
        /// </summary>
        public CostEstimateDataModel Data { get; set; } = default!;
        
        /// <summary>
        /// Suma całkowita netto (obliczana)
        /// </summary>
        public decimal? TotalNet { get; set; }
        
        /// <summary>
        /// Suma całkowita brutto (obliczana)
        /// </summary>
        public decimal? TotalGross { get; set; }
        
        /// <summary>
        /// Data utworzenia
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Data ostatniej aktualizacji
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
        
        /// <summary>
        /// Data ostatniego obliczenia sum
        /// </summary>
        public DateTime? LastCalculatedAt { get; set; }
        
        /// <summary>
        /// Soft delete
        /// </summary>
        public bool IsDeleted { get; set; }
        
        /// <summary>
        /// Data usunięcia
        /// </summary>
        public DateTime? DeletedAt { get; set; }
        
        // Navigation properties
        
        /// <summary>
        /// Tenant
        /// </summary>
        public virtual Tenant Tenant { get; set; } = default!;
        
        /// <summary>
        /// Projekt
        /// </summary>
        public virtual Project Project { get; set; } = default!;
        
        /// <summary>
        /// Szablon kosztorysu (definicja struktury)
        /// </summary>
        public virtual CostEstimateTemplate Template { get; set; } = default!;
        
        /// <summary>
        /// Właściciel kosztorysu
        /// </summary>
        public virtual User Owner { get; set; } = default!;
    }
    
    /// <summary>
    /// Status kosztorysu
    /// </summary>
    public enum CostEstimateStatus
    {
        /// <summary>
        /// Wersja robocza
        /// </summary>
        Draft = 0,
        
        /// <summary>
        /// W trakcie wypełniania
        /// </summary>
        InProgress = 1,
        
        /// <summary>
        /// Gotowy do przeglądu
        /// </summary>
        ReadyForReview = 2,
        
        /// <summary>
        /// Zatwierdzony
        /// </summary>
        Approved = 3,
        
        /// <summary>
        /// Odrzucony
        /// </summary>
        Rejected = 4,
        
        /// <summary>
        /// Zarchiwizowany
        /// </summary>
        Archived = 5
    }
}
