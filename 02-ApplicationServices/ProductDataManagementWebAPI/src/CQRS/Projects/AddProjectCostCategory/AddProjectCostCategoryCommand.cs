using Business.Interfaces.Constants;
using Business.Interfaces.Model;

namespace CQRS.Projects.AddProjectCostCategory
{
    public sealed record AddProjectCostCategoryCommand : IRequestCommand<Guid>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required string Name { get; init; }
        public string? Code { get; init; }
        public string? Color { get; init; }

        public string PermissionCode => PermissionCodes.ProjectSettings;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
