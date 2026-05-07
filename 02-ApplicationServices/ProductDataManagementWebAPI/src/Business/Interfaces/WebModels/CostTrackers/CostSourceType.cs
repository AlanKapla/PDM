using System.Text.Json.Serialization;

namespace Business.Interfaces.WebModels.CostTrackers
{
    /// <summary>
    /// Źródło kosztu w kontekście dashboardu projektu.
    /// Wartość jest wyznaczana przy mapowaniu do DTO (nie przechowywana w bazie).
    ///
    /// Logika rozstrzygania (Resolve):
    /// <code>
    ///   if (CostEstimateItemId != null &amp;&amp; WorkScheduleStageWorkId != null)
    ///       return LinkedWorkItem;  // koszt wspólny — pozycja kosztorysu i zakres pracy
    ///
    ///   if (WorkScheduleStageWorkId != null)
    ///       return ScheduleWorkItem;
    ///
    ///   if (CostEstimateItemId != null)
    ///       return EstimateItem;
    ///
    ///   return ProjectAdditional;
    /// </code>
    ///
    /// ProjectAdditional — workItemLinkId = null, costEstimateItemId = null, workScheduleStageWorkId = null.
    /// ScheduleWorkItem  — workItemLinkId = null, workScheduleStageWorkId != null, costEstimateItemId = null.
    /// EstimateItem      — workItemLinkId = null, costEstimateItemId != null, workScheduleStageWorkId = null.
    /// LinkedWorkItem    — workItemLinkId != null lub oba: costEstimateItemId i workScheduleStageWorkId.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CostSourceType
    {
        /// <summary>Koszt dodatkowy projektu — nieprzypisany do żadnej pozycji.</summary>
        ProjectAdditional = 0,

        /// <summary>Koszt powiązany wyłącznie z zakresem prac harmonogramu (bez kosztorysu).</summary>
        ScheduleWorkItem = 1,

        /// <summary>Koszt powiązany wyłącznie z pozycją kosztorysu (bez harmonogramu).</summary>
        EstimateItem = 2,

        /// <summary>Koszt powiązany jednocześnie z harmonogramem i kosztorysem (przez link lub oba FK).</summary>
        LinkedWorkItem = 3
    }
}
