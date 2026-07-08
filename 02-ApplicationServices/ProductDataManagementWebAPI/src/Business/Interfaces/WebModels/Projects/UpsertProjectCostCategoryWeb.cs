namespace Business.Interfaces.WebModels.Projects
{
    public sealed record UpsertProjectCostCategoryWeb
    {
        public required string Name { get; init; }
        public string? Code { get; init; }
        public int Order { get; init; }
        public string? Color { get; init; }
    }
}
