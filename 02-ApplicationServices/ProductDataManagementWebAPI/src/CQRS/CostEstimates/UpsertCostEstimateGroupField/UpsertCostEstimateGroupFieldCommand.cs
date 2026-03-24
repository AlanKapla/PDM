using Business.Interfaces.Constants;
using Business.Interfaces.Model;

namespace CQRS.CostEstimates.UpsertCostEstimateGroupField
{
    /// <summary>
    /// Command to add or update a group field value.
    /// When FieldValueId is null a new field value is created (FieldDefinitionId is required).
    /// When FieldValueId is provided the existing field value is updated.
    /// </summary>
    public sealed record UpsertCostEstimateGroupFieldCommand : IRequestCommand<Guid>, IAuthorizableRequest
    {
        public Guid CostEstimateId { get; init; }
        public Guid GroupId { get; init; }
        public Guid? FieldValueId { get; init; }
        public Guid? FieldDefinitionId { get; init; }
        public string? StringValue { get; init; }
        public decimal? DecimalValue { get; init; }
        public bool? BoolValue { get; init; }
        public DateTime? DateTimeValue { get; init; }
        public Guid TenantId { get; init; }
        public Guid ProjectId { get; init; }

        public string PermissionCode => PermissionCodes.ProjectResourcesWriteShared;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
