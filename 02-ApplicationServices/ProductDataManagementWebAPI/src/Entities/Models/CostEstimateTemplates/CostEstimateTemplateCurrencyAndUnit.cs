using Entities.Models.Base;

namespace Entities.Models.CostEstimateTemplates
{
    /// <summary>
    /// Waluta dostępna w wersji szablonu kosztorysu
    /// </summary>
    public class CostEstimateTemplateCurrency : BaseEntity
    {
        /// <summary>
        /// ID wersji szablonu kosztorysu
        /// </summary>
        public Guid TemplateVersionId { get; set; }
        
        /// <summary>
        /// Kod waluty (np. "PLN", "USD", "EUR")
        /// </summary>
        public string Code { get; set; } = default!;
        
        /// <summary>
        /// Nazwa waluty (np. "Polski złoty", "US Dollar", "Euro")
        /// </summary>
        public string Name { get; set; } = default!;
        
        /// <summary>
        /// Symbol waluty (np. "zł", "$", "€")
        /// </summary>
        public string? Symbol { get; set; }
        
        /// <summary>
        /// Czy jest to waluta domyślna dla nowych kosztorysów
        /// </summary>
        public bool IsDefault { get; set; }
        
        /// <summary>
        /// Kolejność wyświetlania
        /// </summary>
        public int Order { get; set; }
        
        // Navigation properties
        
        /// <summary>
        /// Wersja szablonu kosztorysu
        /// </summary>
        public virtual CostEstimateTemplateVersion TemplateVersion { get; set; } = default!;
    }
    
    /// <summary>
    /// Jednostka miary dostępna w wersji szablonu kosztorysu
    /// </summary>
    public class CostEstimateTemplateUnit : BaseEntity
    {
        /// <summary>
        /// ID wersji szablonu kosztorysu
        /// </summary>
        public Guid TemplateVersionId { get; set; }
        
        /// <summary>
        /// Kod jednostki (np. "szt", "m2", "mb", "kg")
        /// </summary>
        public string Code { get; set; } = default!;
        
        /// <summary>
        /// Pełna nazwa jednostki (np. "sztuka", "metr kwadratowy", "metr bieżący", "kilogram")
        /// </summary>
        public string Name { get; set; } = default!;
        
        /// <summary>
        /// Symbol jednostki do wyświetlania (np. "szt", "m²", "mb", "kg")
        /// </summary>
        public string Symbol { get; set; } = default!;
        
        /// <summary>
        /// Kategoria jednostki (np. "Długość", "Powierzchnia", "Objętość", "Masa", "Czas", "Ilość")
        /// </summary>
        public string? Category { get; set; }
        
        /// <summary>
        /// Czy jest to jednostka domyślna dla pola Quantity
        /// </summary>
        public bool IsDefault { get; set; }
        
        /// <summary>
        /// Kolejność wyświetlania
        /// </summary>
        public int Order { get; set; }
        
        // Navigation properties
        
        /// <summary>
        /// Wersja szablonu kosztorysu
        /// </summary>
        public virtual CostEstimateTemplateVersion TemplateVersion { get; set; } = default!;
    }
}
