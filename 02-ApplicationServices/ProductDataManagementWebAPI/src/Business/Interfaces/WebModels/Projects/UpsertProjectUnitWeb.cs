namespace Business.Interfaces.WebModels.Projects
{
    public sealed record UpsertProjectUnitWeb
    {
        public required string Code { get; init; }
        public required string Name { get; init; }
        public string? Symbol { get; init; }
        public int Order { get; init; }
    }
}
