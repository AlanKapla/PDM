using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Projects.ReorderProjectUnits
{
    public sealed record ReorderProjectUnitsCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required List<Guid> UnitIds { get; init; }

        public string PermissionCode => PermissionCodes.ProjectSettings;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
