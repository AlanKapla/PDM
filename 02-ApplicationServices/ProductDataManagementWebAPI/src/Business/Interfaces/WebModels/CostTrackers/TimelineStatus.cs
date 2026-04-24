namespace Business.Interfaces.WebModels.CostTrackers
{
    /// <summary>
    /// Status czasowy węzła — stan realizacji względem harmonogramu.
    /// Obliczany na każdym poziomie drzewa (zakres pracy, etap, harmonogram, projekt).
    /// Na poziomach bez harmonogramu zawsze = NoSchedule.
    /// </summary>
    public enum TimelineStatus
    {
        /// <summary>Brak powiązanego harmonogramu.</summary>
        NoSchedule    = 0,
        /// <summary>Zaplanowane, ReferenceDate przed PlannedStart.</summary>
        NotStarted    = 1,
        /// <summary>W toku, ReferenceDate między PlannedStart a PlannedEnd.</summary>
        InProgress    = 2,
        /// <summary>W toku lub nierozpoczęte, ReferenceDate po PlannedEnd.</summary>
        Delayed       = 3,
        /// <summary>Ukończone, zakończone przed lub w dniu PlannedEnd.</summary>
        Completed     = 4,
        /// <summary>Ukończone, ale po PlannedEnd (z opóźnieniem).</summary>
        CompletedLate = 5,
        /// <summary>Brak konfiguracji harmonogramu — brak etapów, zakresów pracy lub okresów.</summary>
        NotConfigured = 6,
    }
}
