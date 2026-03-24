using Business.Interfaces.WebModels.CostEstimateTemplates;

namespace Business.Implementation.Helpers
{
    /// <summary>
    /// Internal model for deserializing default template JSON files.
    /// Reuses existing DTOs for currencies, units and field definitions.
    /// </summary>
    internal sealed record DefaultTemplateJson
    {
        public string Slug { get; init; } = default!;
        public string Name { get; init; } = default!;
        public string? Description { get; init; }
        public string? Category { get; init; }
        public Guid TemplateId { get; init; }
        public int? MaxGroupLevel { get; init; }
        public List<CurrencyDto> Currencies { get; init; } = [];
        public List<UnitDto> Units { get; init; } = [];
        public List<CategoryDto> Categories { get; init; } = [];
        public List<FieldDefinitionDto> GroupHeaderFields { get; init; } = [];
        public List<FieldDefinitionDto> SystemFields { get; init; } = [];
        public List<FieldDefinitionDto> CalculatedFields { get; init; } = [];
        public List<FieldDefinitionDto> GenericFields { get; init; } = [];
    }
}
