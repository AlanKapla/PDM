using Business.Interfaces.Constants;
using Entities.Models.CostEstimates;
using MediatR;

namespace CQRS.CostEstimates.UpdateAdditionalField
{
    /// <summary>
    /// Command aktualizujący pole dodatkowe kosztorysu.
    /// Aktualizuje tylko nie-null properties.
    /// </summary>
    public sealed record UpdateAdditionalFieldCommand : CostEstimateCommandBase, IRequestCommand<Unit>
    {
        /// <summary>
        /// ID pola dodatkowego do zaktualizowania
        /// </summary>
        public Guid FieldId { get; init; }

        /// <summary>
        /// Nowa nazwa pola (opcjonalnie)
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        /// Nowy typ pola (opcjonalnie)
        /// </summary>
        public AdditionalFieldType? FieldType { get; init; }

        /// <summary>
        /// Nowa kolejność wyświetlania (opcjonalnie)
        /// </summary>
        public int? Order { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
