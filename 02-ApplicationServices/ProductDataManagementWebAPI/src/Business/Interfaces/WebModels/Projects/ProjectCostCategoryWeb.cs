namespace Business.Interfaces.WebModels.Projects
{
    public sealed record ProjectCostCategoryWeb
    {
        public required Guid Id { get; init; }
        public required string Name { get; init; }
        public string? Code { get; init; }
        public required int Order { get; init; }
        public string? Color { get; init; }
    }
}
