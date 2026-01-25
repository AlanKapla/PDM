namespace Business.Interfaces.WebModels.CostEstimateTemplates
{
    /// <summary>
    /// Result DTO for approved template version
    /// Zwraca podstawowe informacje o zatwierdzonej wersji szablonu wraz z dostępnymi walutami i jednostkami
    /// Szczegółowe definicje pól są pobierane przez dedykowane endpointy
    /// </summary>
    public record ApprovedTemplateVersionItemWeb(
        Guid VersionId,
        Guid TemplateId,
        string TemplateName,
        string? TemplateCategory,
        int VersionNumber,
        string? VersionName,
        DateTime ApprovedAt,
        string? ApprovedByUserName,
        bool CanAddGroups,
        bool CanBranchGroups,
        int? MaxGroupLevel,
        List<TemplateCurrencyWeb> Currencies,
        List<TemplateUnitWeb> Units
    );
    
    /// <summary>
    /// Waluta dostępna w szablonie
    /// </summary>
    public record TemplateCurrencyWeb(
        Guid Id,
        string Code,
        string Name,
        string? Symbol,
        bool IsDefault,
        int Order
    );
    
    /// <summary>
    /// Jednostka miary dostępna w szablonie
    /// </summary>
    public record TemplateUnitWeb(
        Guid Id,
        string Code,
        string Name,
        string Symbol,
        string? Category,
        bool IsDefault,
        int Order
    );
}
