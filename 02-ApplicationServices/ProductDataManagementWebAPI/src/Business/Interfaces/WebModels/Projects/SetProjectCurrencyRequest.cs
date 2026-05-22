namespace Business.Interfaces.WebModels.Projects
{
    public sealed record SetProjectCurrencyRequest
    {
        public required string Code { get; init; }
        public required string Name { get; init; }
        public string? Symbol { get; init; }
    }
}
