using Entities.Models.Base;
using Entities.Models.CostEstimates;

namespace Entities.Models.CostEstimateTemplates
{
    /// <summary>
    /// Wersja szablonu kosztorysu - przechowuje konfigurację pól dla danej wersji
    /// </summary>
    public class CostEstimateTemplateVersion : BaseEntity
    {
        /// <summary>
        /// ID szablonu kosztorysu
        /// </summary>
        public Guid TemplateId { get; set; }
        
        /// <summary>
        /// Numer wersji (1, 2, 3, ...)
        /// </summary>
        public int VersionNumber { get; set; }
        
        /// <summary>
        /// Nazwa wersji (opcjonalna, np. "Wersja początkowa", "Dodano pola materiałów")
        /// </summary>
        public string? VersionName { get; set; }
        
        /// <summary>
        /// Opis zmian w tej wersji
        /// </summary>
        public string? ChangeDescription { get; set; }
        
        /// <summary>
        /// Status wersji
        /// </summary>
        public TemplateVersionStatus Status { get; set; }
        
        /// <summary>
        /// Kategoria szablonu (np. "Budowa", "Remont", "Wyposażenie")
        /// </summary>
        public string? Category { get; set; }
        
        /// <summary>
        /// Czy można dodawać nowe grupy podczas wypełniania kosztorysu
        /// </summary>
        public bool CanAddGroups { get; set; }
        
        /// <summary>
        /// Czy można rozgałęziać grupy (tworzyć podgrupy)
        /// </summary>
        public bool CanBranchGroups { get; set; }
        
        /// <summary>
        /// Maksymalny poziom zagnieżdżenia grup (null = bez limitu)
        /// </summary>
        public int? MaxGroupLevel { get; set; }
        
        /// <summary>
        /// Czy automatycznie numerować grupy
        /// </summary>
        public bool AutoNumberGroups { get; set; }
        
        /// <summary>
        /// Format numeracji grup (np. "{0}" dla "1", "Etap {0}" dla "Etap 1", "{0:00}" dla "01")
        /// </summary>
        public string? GroupNumberFormat { get; set; }
        
        /// <summary>
        /// Data utworzenia wersji
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Data zatwierdzenia wersji (gdy status zmienia się na Approved)
        /// </summary>
        public DateTime? ApprovedAt { get; set; }
        
        /// <summary>
        /// ID użytkownika, który zatwierdził wersję
        /// </summary>
        public Guid? ApprovedById { get; set; }
        
        /// <summary>
        /// Data oznaczenia jako Deprecated (gdy nowsza wersja zostaje zatwierdzona)
        /// </summary>
        public DateTime? DeprecatedAt { get; set; }
        
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
        /// Szablon kosztorysu
        /// </summary>
        public virtual CostEstimateTemplate Template { get; set; } = default!;
        
        /// <summary>
        /// Użytkownik, który zatwierdził wersję
        /// </summary>
        public virtual User? ApprovedBy { get; set; }
        
        /// <summary>
        /// Waluty dostępne w tej wersji szablonu
        /// </summary>
        public virtual ICollection<CostEstimateTemplateCurrency> Currencies { get; set; } = new List<CostEstimateTemplateCurrency>();
        
        /// <summary>
        /// Jednostki miary dostępne w tej wersji szablonu
        /// </summary>
        public virtual ICollection<CostEstimateTemplateUnit> Units { get; set; } = new List<CostEstimateTemplateUnit>();
        
        /// <summary>
        /// Definicje pól nagłówka grupy
        /// </summary>
        public virtual ICollection<CostEstimateTemplateGroupFieldDefinition> GroupFieldDefinitions { get; set; } = new List<CostEstimateTemplateGroupFieldDefinition>();
        
        /// <summary>
        /// Definicje pól systemowych pozycji (Nazwa, Ilość, Jednostka)
        /// </summary>
        public virtual ICollection<CostEstimateTemplateItemSystemFieldDefinition> SystemFieldDefinitions { get; set; } = new List<CostEstimateTemplateItemSystemFieldDefinition>();
        
        /// <summary>
        /// Definicje pól obliczeniowych pozycji
        /// </summary>
        public virtual ICollection<CostEstimateTemplateItemCalculatedFieldDefinition> CalculatedFieldDefinitions { get; set; } = new List<CostEstimateTemplateItemCalculatedFieldDefinition>();
        
        /// <summary>
        /// Definicje pól generycznych pozycji
        /// </summary>
        public virtual ICollection<CostEstimateTemplateItemGenericFieldDefinition> GenericFieldDefinitions { get; set; } = new List<CostEstimateTemplateItemGenericFieldDefinition>();
    }
}
