using Business.Interfaces.Constants;
using MediatR;

namespace CQRS.CostEstimates.DeleteAdditionalField
{
    /// <summary>
    /// Command usuwający pole dodatkowe z kosztorysu.
    /// Usuwa fizycznie pole oraz wszystkie jego wartości (kaskada).
    /// Po usunięciu reorganizuje Order pozostałych pól.
    /// </summary>
    public sealed record DeleteAdditionalFieldCommand : CostEstimateCommandBase, IRequestCommand<Unit>
    {
        /// <summary>
        /// ID pola dodatkowego do usunięcia
        /// </summary>
        public Guid FieldId { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
