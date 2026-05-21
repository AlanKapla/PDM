namespace Business.Interfaces.WebModels.CostEstimates
{
    public sealed record ShareCostEstimateRequestWeb(List<Guid> UserIds);

    public sealed record UpdateCostEstimateSharesRequestWeb(List<Guid> UserIds);

    public sealed record CostEstimateShareWeb(
        Guid UserId,
        string FullName,
        string Email,
        DateTime SharedAt
    );
}
