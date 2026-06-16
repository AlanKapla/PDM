using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Entities.Models.Base;
using Entities.Models.CostTrackers;

namespace Entities.Models.CostEstimates
{
    /// <summary>
    /// Pozycja kosztorysu (work scope item).
    /// Może mieć kolekcję opcji (Options) jeśli ma child items z RelationType = Option.
    /// Może mieć kolekcję komponentów (Components) - wtedy wartości są sumowane z komponentów.
    /// Pola podstawowe (Quantity, Unit, UnitPriceNet, etc.) są bezpośrednimi właściwościami.
    /// Pola dodatkowe użytkownika przechowywane w AdditionalFieldValues.
    /// </summary>
    public class CostEstimateItem : DeletableEntity
    {
        public Guid CostEstimateId { get; set; }
        public string Name { get; set; } = default!;
        public Guid GroupId { get; set; }

        /// <summary>
        /// ID pozycji nadrzędnej (parent item)
        /// NULL gdy pozycja jest główna (RelationType = None)
        /// Wypełnione gdy pozycja jest opcją (RelationType = Option) lub komponentem (RelationType = Component)
        /// </summary>
        public Guid? ParentItemId { get; set; }

        /// <summary>
        /// Typ relacji do pozycji nadrzędnej
        /// None = pozycja główna, Option = opcja, Component = komponent
        /// </summary>
        public ItemRelationType RelationType { get; set; }

        public int Order { get; set; }

        // === NOWE POLA PODSTAWOWE (zamiast FieldValues) ===

        /// <summary>
        /// Ilość (decimal)
        /// </summary>
        public decimal? Quantity { get; set; }

        /// <summary>
        /// Jednostka miary (string) — szt, m, m², m³, kg, mb, godz, kpl
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// Cena jednostkowa netto
        /// </summary>
        public decimal? UnitPriceNet { get; set; }

        /// <summary>
        /// Stawka VAT (decimal, zakres 0–1, gdzie 0.23 = 23%)
        /// </summary>
        public decimal? VatRate { get; set; }

        /// <summary>
        /// Cena jednostkowa brutto — obliczana: UnitPriceNet * (1 + VatRate)
        /// </summary>
        public decimal? UnitPriceGross { get; set; }

        /// <summary>
        /// Czy pozycja/opcja/komponent jest wybrana do sumowania:
        /// - RelationType=None: checkbox do sumowania w etapie (default: true)
        /// - RelationType=Option: radio button do wyboru wariantu (exclusive)
        /// - RelationType=Component: checkbox do sumowania w pozycji (default: true)
        /// </summary>
        public bool IsSelected { get; set; } = true;

        /// <summary>
        /// Czy pozycja główna (RelationType=None) ma być dodana jako zakres pracy w harmonogramie.
        /// Tylko dla pozycji głównych — ignorowane dla opcji i komponentów.
        /// </summary>
        public bool IsStageWork { get; set; } = false;

        /// <summary>
        /// Wartość netto pozycji (obliczana)
        /// Jeśli pozycja ma Components - suma z Components, jeśli nie - z pól podstawowych
        /// </summary>
        public decimal? NetValue { get; set; }

        /// <summary>
        /// Wartość brutto pozycji (obliczana)
        /// Jeśli pozycja ma Components - suma z Components, jeśli nie - z pól podstawowych
        /// </summary>
        public decimal? GrossValue { get; set; }

        /// <summary>
        /// Wartość VAT pozycji (obliczana)
        /// Jeśli pozycja ma Components - suma z Components, jeśli nie - z pól podstawowych
        /// </summary>
        public decimal? VatValue { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual CostEstimate CostEstimate { get; set; } = default!;
        public virtual CostEstimateGroup Group { get; set; } = default!;

        /// <summary>
        /// Pozycja nadrzędna (jeśli ta pozycja jest opcją lub komponentem)
        /// </summary>
        public virtual CostEstimateItem? ParentItem { get; set; }
        public virtual ICollection<TrackedCost>? TrackedCosts { get; set; }

        /// <summary>
        /// Wartości pól dodatkowych dla tej pozycji (nowa płaska struktura)
        /// </summary>
        public virtual ICollection<CostEstimateAdditionalFieldValue> AdditionalFieldValues { get; set; } = new List<CostEstimateAdditionalFieldValue>();

        /// <summary>
        /// Pliki dołączone do tej pozycji (nowa struktura, zastępuje CostEstimateFieldFile)
        /// </summary>
        public virtual ICollection<CostEstimateItemFile> Files { get; set; } = new List<CostEstimateItemFile>();

        /// <summary>
        /// Kolekcja child items (Options + Components razem)
        /// EF nie rozróżnia - musisz filtrować po RelationType w kodzie
        /// </summary>
        private ICollection<CostEstimateItem>? _childItems;

        /// <summary>
        /// Kolekcja opcji (zagnieżdżonych pozycji)
        /// Filtrowane z AllItems (załadowanych przez Include) gdzie ParentItemId == this.Id && RelationType = Option
        /// </summary>
        public ICollection<CostEstimateItem> Options
        {
            get
            {
                if (_childItems == null)
                {
                    return new List<CostEstimateItem>();
                }

                return _childItems.Where(c => c.RelationType == ItemRelationType.Option).ToList();
            }
        }

        /// <summary>
        /// Kolekcja komponentów (składników pozycji)
        /// Wypełniona gdy pozycja główna składa się z komponentów (robocizna, materiał, etc.)
        /// WAŻNE: Pozycja z komponentami NIE MOŻE mieć własnych wartości podstawowych!
        /// Filtrowane z AllItems (załadowanych przez Include) gdzie ParentItemId == this.Id && RelationType = Component
        /// </summary>
        public ICollection<CostEstimateItem> Components
        {
            get
            {
                if (_childItems == null)
                {
                    return new List<CostEstimateItem>();
                }

                return _childItems.Where(c => c.RelationType == ItemRelationType.Component).ToList();
            }
        }

        /// <summary>
        /// Helper method - ustawia child items (używane przez EF lub kod ładujący)
        /// </summary>
        public void SetChildItems(IEnumerable<CostEstimateItem> childItems)
        {
            _childItems = childItems.ToList();
        }
    }
}
