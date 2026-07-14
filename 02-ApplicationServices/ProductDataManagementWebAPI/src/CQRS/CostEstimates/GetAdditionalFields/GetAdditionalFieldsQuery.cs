using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.CostEstimates;

namespace CQRS.CostEstimates.GetAdditionalFields
{
    /// <summary>
    /// Query do pobrania wszystkich pól dodatkowych dla danego kosztorysu.
    /// Zwraca pola posortowane po Order.
    /// </summary>
    public sealed record GetAdditionalFieldsQuery : CostEstimateCommandBase, IRequestQuery<List<CostEstimateAdditionalFieldWeb>>
    {
        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
