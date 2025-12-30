namespace CQRS.CostEstimates.CopyCostEstimate
{
    public record CopyCostEstimateCommand(
        Guid CostEstimateId,
        List<Guid> TargetProjectIds
    ) : IRequestCommand<List<Guid>>
    {
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }
    }
}
