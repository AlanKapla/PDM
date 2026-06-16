using Business.Interfaces.Constants;
using MediatR;

namespace CQRS.CostEstimates.UpdateGroupBaseFields
{
    /// <summary>
    /// Command to update base fields of a cost estimate group.
    /// Only non-null properties are updated. Group has only Name as editable base field.
    /// </summary>
    public sealed record UpdateGroupBaseFieldsCommand : CostEstimateCommandBase, IRequestCommand<Unit>
    {
        public Guid GroupId { get; init; }
        public string? Name { get; init; }
        public bool ClearName { get; init; }

        public override string PermissionCode => PermissionCodes.ProjectEstimates;
    }
}
