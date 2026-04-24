using Entities.Models.Base;
using Entities.Models.CostEstimates;
using Entities.Models.CostTrackers;

namespace Entities.Models.WorkItemLinks
{
    /// <summary>
    /// Łącznik między pozycją kosztorysu a zakresem pracy harmonogramu.
    /// Nośnik kosztów rzeczywistych — TrackedCost wskazuje na ten łącznik.
    /// </summary>
    public class CostEstimateItemWorkScheduleStageWorkLink : BaseEntity
    {
        /// <summary>ID projektu, do którego należy łącznik.</summary>
        public Guid ProjectId { get; set; }

        /// <summary>FK do CostEstimateGroupWorkScheduleStageLink. Nullable — gdy link nie jest powiązany z etapem.</summary>
        public Guid? GroupStageLinkId { get; set; }

        /// <summary>FK do CostEstimateItem. Nullable — link może istnieć bez pozycji kosztorysu.</summary>
        public Guid? CostEstimateItemId { get; set; }

        /// <summary>FK do WorkScheduleStageWork. Nullable — link może istnieć bez zakresu pracy.</summary>
        public Guid? WorkScheduleStageWorkId { get; set; }

        /// <summary>Denormalizowana nazwa — kopiowana z CostEstimateItem.Name lub WorkScheduleStageWork.Name przy tworzeniu.</summary>
        public string DisplayName { get; set; } = default!;

        /// <summary>Denormalizowany budżet netto — kopiowany z CostEstimateItem.NetValue.</summary>
        public decimal? BudgetNet { get; set; }

        /// <summary>Denormalizowany budżet brutto — kopiowany z CostEstimateItem.GrossValue.</summary>
        public decimal? BudgetGross { get; set; }

        /// <summary>Denormalizowana data rozpoczęcia — kopiowana z WorkScheduleStageWork.PlannedStartDate.</summary>
        public DateTime? PlannedStart { get; set; }

        /// <summary>Denormalizowana data zakończenia — kopiowana z WorkScheduleStageWork.PlannedEndDate.</summary>
        public DateTime? PlannedEnd { get; set; }

        /// <summary>Denormalizowany status zamknięcia — true gdy wszystkie okresy zakresu pracy są zamknięte (IsClosed=true). Aktualizowany przy każdej zmianie periodów.</summary>
        public bool IsWorkClosed { get; set; }

        /// <summary>Kolejność wyświetlania w ramach grupy.</summary>
        public int Order { get; set; }

        public virtual CostEstimateGroupWorkScheduleStageLink? GroupStageLink { get; set; }
        public virtual CostEstimateItem? CostEstimateItem { get; set; }
        public virtual WorkScheduleStageWork? WorkScheduleStageWork { get; set; }

        /// <summary>Kolekcja kosztów rzeczywistych przypisanych do tego łącznika.</summary>
        public virtual ICollection<TrackedCost> TrackedCosts { get; set; }
            = new List<TrackedCost>();

        /// <summary>Suma kosztów netto z TrackedCosts. Obliczana w pamięci — nie jest przechowywana w bazie.</summary>
        public decimal? ActualNet => TrackedCosts.Sum(t => t.Net);

        /// <summary>Suma kosztów brutto z TrackedCosts. Obliczana w pamięci — nie jest przechowywana w bazie.</summary>
        public decimal? ActualGross => TrackedCosts.Sum(t => t.Gross);

        /// <summary>Różnica między budżetem netto a kosztami rzeczywistymi. Obliczana w pamięci.</summary>
        public decimal? Variance => BudgetNet - ActualNet;
    }
}
