using Business.Interfaces.Constants;
using Entities.Models.CostEstimates;
using MediatR;

namespace CQRS.CostEstimates.AddAdditionalField
{
    /// <summary>
    /// Command dodający nowe pole dodatkowe do kosztorysu.
    /// Jeśli Order nie jest podany, pole zostanie dodane na koniec.
    /// </summary>
    public sealed record AddAdditionalFieldCommand : CostEstimateCommandBase, IRequestCommand<Guid>
    {
        /// <summary>
        /// Nazwa pola (np. "Kod CPV", "Uwagi")
        /// </summary>
        public string Name { get; init; } = default!;

        /// <summary>
        /// Typ pola dodatkowego (AdditionalFieldType)
        /// </summary>
        public AdditionalFieldType FieldType { get; init; }

        /// <summary>
        /// Kolejność wyświetlania (opcjonalnie, domyślnie na końcu)
        /// </summary>
        public int? Order { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
