namespace Business.Interfaces.WebModels.CostEstimateTemplates
{
    /// <summary>
    /// Result DTO for template details
    /// Zwraca szczegóły szablonu z wybraną wersją i jej pełną strukturą
    /// </summary>
    public record CostEstimateTemplateDetailsWeb(
        Guid Id,
        string Name,
        string? Description,
        string? Category,
        bool CanAddGroups,
        bool CanBranchGroups,
        int? MaxGroupLevel,
        bool AutoNumberGroups,
        string? GroupNumberFormat,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        Guid OwnerId,
        string OwnerName,
        CostEstimateTemplateVersionInfoWeb? SelectedVersion,
        CostEstimateTemplateVersionStructureWeb? VersionStructure
    );
}
