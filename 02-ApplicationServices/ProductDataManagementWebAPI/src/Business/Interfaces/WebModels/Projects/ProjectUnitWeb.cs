namespace Business.Interfaces.WebModels.Projects
{
    public sealed record ProjectUnitWeb
    {
        public required Guid Id { get; init; }
        public required string Code { get; init; }
        public required string Name { get; init; }
        public string? Symbol { get; init; }
        public required int Order { get; init; }
    }
}
