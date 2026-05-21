using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;

namespace CQRS.Projects.UpdateProject
{
    public sealed record UpdateProjectCommand : IRequestCommand<ProjectDetailsWeb>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required Guid ProjectId { get; init; }
        public required string Name { get; init; }

        public string PermissionCode => PermissionCodes.ProjectEdit;

        public ResourceRef GetResource() => new(TenantId: TenantId, ProjectId: ProjectId);
    }
}
