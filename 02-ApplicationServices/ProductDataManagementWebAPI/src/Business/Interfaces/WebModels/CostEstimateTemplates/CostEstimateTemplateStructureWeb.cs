namespace Business.Interfaces.WebModels.CostEstimateTemplates
{
    public record CostEstimateTemplateStructureWeb(
        bool CanAddGroups,
        bool CanBranchGroups,
        int? MaxGroupLevel,
        bool AutoNumberGroups,
        string? GroupNumberFormat,
        List<CurrencyWeb> Currencies,
        List<UnitWeb> Units,
        List<FieldDefinitionWeb> GroupHeaderFields,
        List<FieldDefinitionWeb> SystemFields,
        List<FieldDefinitionWeb> CalculatedFields,
        List<FieldDefinitionWeb> GenericFields,
        SummaryConfigurationWeb? SummaryConfiguration,
        UiConfigurationWeb? UiConfiguration
    );
}
