namespace CQRS.CostEstimateTemplates.CreateCostEstimateTemplate
{
    /// <summary>
    /// Command do tworzenia szablonu kosztorysu
    /// Tworzy tylko minimalny szablon (nazwa, opis) i pierwszą wersję Draft
    /// Cała struktura (pola, waluty, jednostki) jest dodawana przez UpdateCostEstimateTemplate
    /// </summary>
    public record CreateCostEstimateTemplateCommand(
        string Name,
        string? Description
    ) : IRequestCommand<Guid>;
}
