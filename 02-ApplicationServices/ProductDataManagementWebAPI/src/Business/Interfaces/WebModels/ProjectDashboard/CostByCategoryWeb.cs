namespace Business.Interfaces.WebModels.ProjectDashboard
{
    public sealed record CostByCategoryWeb
    {
        public Guid? CategoryId { get; init; }
        public required string CategoryName { get; init; }
        public string? Color { get; init; }
        public decimal Net { get; init; }
        public decimal? Gross { get; init; }
        public int CostsCount { get; init; }
    }
}
