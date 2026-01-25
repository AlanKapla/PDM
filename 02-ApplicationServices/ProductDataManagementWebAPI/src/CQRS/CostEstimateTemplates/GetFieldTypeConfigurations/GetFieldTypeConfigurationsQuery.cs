using Business.Interfaces.WebModels.CostEstimateTemplates;

namespace CQRS.CostEstimateTemplates.GetFieldTypeConfigurations
{
    /// <summary>
    /// Query do pobrania konfiguracji wszystkich dostępnych typów pól
    /// Zwraca słownik zgrupowany według FieldScope
    /// </summary>
    public record GetFieldTypeConfigurationsQuery() : IRequestQuery<Dictionary<int, CostEstimateFieldTypeConfigWeb[]>>;
}
