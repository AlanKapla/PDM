using Entities.Models.Base;

namespace Entities.Models.CostEstimateTemplates
{
    /// <summary>
    /// Szablon kosztorysu - definicja struktury kosztorysu wielokrotnego użytku
    /// </summary>
    public class CostEstimateTemplate : BaseEntity
    {
        /// <summary>
        /// ID właściciela szablonu (User)
        /// </summary>
        public Guid OwnerId { get; set; }
        
        /// <summary>
        /// Nazwa szablonu
        /// </summary>
        public string Name { get; set; } = default!;
        
        /// <summary>
        /// Opis szablonu
        /// </summary>
        public string? Description { get; set; }
        
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
        /// Data utworzenia
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Data ostatniej aktualizacji
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
        
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
        /// Właściciel szablonu
        /// </summary>
        public virtual User Owner { get; set; } = default!;
        
        /// <summary>
        /// Waluty dostępne w szablonie
        /// </summary>
        public virtual ICollection<CostEstimateTemplateCurrency> Currencies { get; set; } = new List<CostEstimateTemplateCurrency>();
        
        /// <summary>
        /// Jednostki miary dostępne w szablonie
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
