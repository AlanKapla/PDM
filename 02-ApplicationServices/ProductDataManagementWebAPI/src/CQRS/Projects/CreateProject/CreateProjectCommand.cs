using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;

namespace CQRS.Projects.CreateProject
{
    public sealed record CreateProjectCommand : IRequestCommand<ProjectDetailsWeb>, IAuthorizableRequest
    {
        public required Guid TenantId { get; init; }
        public required string Name { get; init; }

        public string PermissionCode => PermissionCodes.TenantProjectsCreate;

        public ResourceRef GetResource()
        {
            return new ResourceRef(TenantId: TenantId);
        }
    }
}
