using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Projects.UpdateProjectCostCategory
{
    public sealed record UpdateProjectCostCategoryCommand : IRequestCommand<MediatR.Unit>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required Guid CategoryId { get; init; }
        public required string Name { get; init; }
        public string? Code { get; init; }
        public required int Order { get; init; }
        public string? Color { get; init; }

        public string PermissionCode => PermissionCodes.ProjectSettings;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
