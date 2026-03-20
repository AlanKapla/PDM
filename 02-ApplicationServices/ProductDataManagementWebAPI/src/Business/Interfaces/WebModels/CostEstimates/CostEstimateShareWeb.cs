namespace Business.Interfaces.WebModels.CostEstimates
{
    public record ShareCostEstimateRequestWeb(List<Guid> UserIds);

    public record UpdateCostEstimateSharesRequestWeb(List<Guid> UserIds);

    public record CostEstimateShareWeb(
        Guid UserId,
        string FullName,
        string Email,
        DateTime SharedAt
    );
}
