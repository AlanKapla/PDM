using Business.Interfaces.Constants;
using Business.Interfaces.Model;

namespace CQRS.CostEstimates.UpsertCostEstimateItemField
{
    /// <summary>
    /// Command to add or update an item field value.
    /// When FieldValueId is null a new field value is created (FieldDefinitionId is required).
    /// When FieldValueId is provided the existing field value is updated.
    /// Works for main items, options, and components.
    /// </summary>
    public sealed record UpsertCostEstimateItemFieldCommand : IRequestCommand<Guid>, IAuthorizableRequest
    {
        public Guid CostEstimateId { get; init; }
        public Guid ItemId { get; init; }
        public Guid? FieldValueId { get; init; }
        public Guid? FieldDefinitionId { get; init; }
        public string? StringValue { get; init; }
        public decimal? DecimalValue { get; init; }
        public bool? BoolValue { get; init; }
        public DateTime? DateTimeValue { get; init; }
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWrite;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
