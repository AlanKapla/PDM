namespace Business.Interfaces.WebModels.AI
{
    /// <summary>
    /// Żądanie zapisu kosztorysu zatwierdzonego przez użytkownika.
    /// Zawiera preview z AI + ostateczna nazwa/opis edytowane przez użytkownika.
    /// </summary>
    public sealed record CreateCostEstimateFromAIPreviewWeb
    {
        /// <summary>Ostateczna nazwa kosztorysu (użytkownik mógł ją zmodyfikować).</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>Ostateczny opis kosztorysu (opcjonalny).</summary>
        public string? Description { get; init; }

        /// <summary>Preview zatwierdzony przez użytkownika (niezmieniony lub po edycji w UI).</summary>
        public AICostEstimatePreviewWeb Preview { get; init; } = default!;
    }
}
