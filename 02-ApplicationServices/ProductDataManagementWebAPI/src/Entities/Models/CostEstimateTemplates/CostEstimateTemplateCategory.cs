using Entities.Models.Base;

namespace Entities.Models.CostEstimateTemplates
{
    /// <summary>
    /// Kategoria dostępna w szablonie kosztorysu.
    /// Użytkownik może wybrać kategorię z listy lub wpisać własną podczas dodawania pozycji.
    /// </summary>
    public class CostEstimateTemplateCategory : BaseEntity
    {
        /// <summary>
        /// ID szablonu kosztorysu
        /// </summary>
        public Guid TemplateId { get; set; }

        /// <summary>
        /// Nazwa kategorii (np. "Robocizna", "Materiały", "Sprzęt")
        /// </summary>
        public string Name { get; set; } = default!;

        /// <summary>
        /// Symbol kategorii (np. "R", "M", "S")
        /// </summary>
        public string? Symbol { get; set; }

        /// <summary>
        /// Kolejność wyświetlania
        /// </summary>
        public int Order { get; set; }

        // Navigation properties

        /// <summary>
        /// Szablon kosztorysu
        /// </summary>
        public virtual CostEstimateTemplate Template { get; set; } = default!;
    }
}
