using Business.Interfaces.Constants;

namespace CQRS.CostEstimates.CreateCostEstimate
{
    /// <summary>
    /// Command do tworzenia kosztorysu z domyślnym schematem (9 podstawowych pól).
    /// </summary>
    public sealed record CreateCostEstimateCommand : CostEstimateRequestBase, IRequestCommand<Guid>
    {
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
