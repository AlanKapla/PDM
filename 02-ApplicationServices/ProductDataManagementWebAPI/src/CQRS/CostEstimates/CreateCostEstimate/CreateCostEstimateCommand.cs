using Business.Interfaces.Constants;

namespace CQRS.CostEstimates.CreateCostEstimate
{
    /// <summary>
    /// Command do tworzenia kosztorysu na bazie wybranego szablonu.
    /// </summary>
    public sealed record CreateCostEstimateCommand : CostEstimateRequestBase, IRequestCommand<Guid>
    {
        public Guid TemplateId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
