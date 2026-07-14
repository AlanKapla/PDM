using Business.Interfaces.Constants;
using MediatR;

namespace CQRS.CostEstimates.UpdateItemBaseFields
{
    /// <summary>
    /// Command to update base fields of a cost estimate item.
    /// Only non-null properties are updated.
    /// Triggers recalculation if a financial field is changed.
    /// </summary>
    public sealed record UpdateItemBaseFieldsCommand : CostEstimateCommandBase, IRequestCommand<Unit>
    {
        public Guid ItemId { get; init; }
        public string? Name { get; init; }
        public decimal? Quantity { get; init; }
        public string? Unit { get; init; }
        public decimal? UnitPriceNet { get; init; }
        public decimal? VatRate { get; init; }
        public decimal? NetValue { get; init; }
        public decimal? GrossValue { get; init; }
        public decimal? VatValue { get; init; }
        public decimal? UnitPriceGross { get; init; }
        public bool ClearName { get; init; }
        public bool ClearQuantity { get; init; }
        public bool ClearUnit { get; init; }
        public bool ClearUnitPriceNet { get; init; }
        public bool ClearVatRate { get; init; }
        public bool ClearNetValue { get; init; }
        public bool ClearGrossValue { get; init; }
        public bool ClearVatValue { get; init; }
        public bool ClearUnitPriceGross { get; init; }
        public bool? IsSelected { get; init; }
        public bool? IsStageWork { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
