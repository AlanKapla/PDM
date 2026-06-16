using Business.Interfaces.Constants;
using MediatR;

namespace CQRS.CostEstimates.ReorderAdditionalFields
{
    /// <summary>
    /// Command zmieniający kolejność pól dodatkowych w kosztorysie.
    /// Kolejność ID na liście określa nowy Order (indeks w liście = Order, 0-based).
    /// </summary>
    public sealed record ReorderAdditionalFieldsCommand : CostEstimateCommandBase, IRequestCommand<Unit>
    {
        /// <summary>
        /// Lista ID pól dodatkowych w nowej kolejności.
        /// Indeks w liście = nowy Order (0-based).
        /// </summary>
        public List<Guid> FieldIds { get; init; } = new();

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
