using Business.Interfaces.WebModels.CostTrackers;

namespace Business.Interfaces.Services
{
    public interface ICostTrackerTimelineService
    {
        /// <summary>
        /// Oblicza TimelineStatus dla pojedynczego zakresu pracy na podstawie dat i ReferenceDate.
        /// Zwraca NoSchedule gdy brak powiązanego WorkScheduleStageWork lub brak dat.
        /// </summary>
        TimelineStatus ComputeItemStatus(DateTime? plannedStart, DateTime? plannedEnd, DateTime referenceDate);

        /// <summary>
        /// Agreguje TimelineStatus w górę drzewa według priorytetu:
        /// Delayed > CompletedLate > InProgress > NotStarted > Completed > NoSchedule.
        /// Wyjątek: wszystkie == Completed → zwraca Completed.
        /// Zwraca NoSchedule gdy kolekcja jest pusta lub wszystkie = NoSchedule.
        /// </summary>
        TimelineStatus AggregateStatuses(IEnumerable<TimelineStatus> statuses);

        /// <summary>
        /// Buduje TimelineStatsWeb z listy węzłów mających harmonogram.
        /// Zwraca null gdy żaden węzeł nie ma HasLinkedSchedule = true.
        /// </summary>
        TimelineStatsWeb? BuildTimelineStats(IReadOnlyList<WorkItemLinkWeb> linkedItems, DateTime referenceDate);

        /// <summary>
        /// Agreguje TimelineStatsWeb z kolekcji TimelineStatsWeb dzieci (grupy, kosztorysy).
        /// Zwraca null gdy wszystkie dzieci mają Timeline = null.
        /// </summary>
        TimelineStatsWeb? AggregateTimelineStats(IEnumerable<TimelineStatsWeb?> childStats, DateTime referenceDate);
    }
}
