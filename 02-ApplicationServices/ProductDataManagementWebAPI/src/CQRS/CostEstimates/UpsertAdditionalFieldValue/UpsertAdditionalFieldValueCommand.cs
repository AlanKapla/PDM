using Business.Interfaces.Constants;
using MediatR;

namespace CQRS.CostEstimates.UpsertAdditionalFieldValue
{
    /// <summary>
    /// Command to upsert a value for an additional field on a group or item.
    /// When GroupId is set, the value is for the group. When ItemId is set,
    /// the value is for the item. Exactly one of GroupId/ItemId must be set.
    /// </summary>
    public sealed record UpsertAdditionalFieldValueCommand : CostEstimateCommandBase, IRequestCommand<Guid>
    {
        /// <summary>
        /// ID definicji pola dodatkowego (CostEstimateAdditionalField)
        /// </summary>
        public Guid AdditionalFieldId { get; init; }

        /// <summary>
        /// ID grupy (gdy wartość jest dla grupy)
        /// </summary>
        public Guid? GroupId { get; init; }

        /// <summary>
        /// ID pozycji (gdy wartość jest dla pozycji)
        /// </summary>
        public Guid? ItemId { get; init; }

        /// <summary>
        /// Wartość tekstowa (dla AdditionalFieldType.String)
        /// </summary>
        public string? StringValue { get; init; }

        /// <summary>
        /// Wartość liczbowa (dla AdditionalFieldType.Decimal)
        /// </summary>
        public decimal? DecimalValue { get; init; }

        /// <summary>
        /// Wartość logiczna (dla AdditionalFieldType.Boolean)
        /// </summary>
        public bool? BoolValue { get; init; }

        /// <summary>
        /// Wartość daty/czasu (dla AdditionalFieldType.DateTime)
        /// </summary>
        public DateTime? DateTimeValue { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
