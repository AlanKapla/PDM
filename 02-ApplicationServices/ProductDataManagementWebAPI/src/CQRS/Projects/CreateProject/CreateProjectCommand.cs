using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Projects;
using CQRS.Behaviours;

namespace CQRS.Projects.CreateProject
{
    public sealed record CreateProjectCommand : IRequestCommand<ProjectDetailsWeb>, IAuthorizableRequest, IRequiresProjectSlot
    {
        public Guid TenantId { get; init; }
        public required string Name { get; init; }

        public string PermissionCode => PermissionCodes.TenantProjectCreate;

        public ResourceRef GetResource()
        {
            return new ResourceRef(TenantId: TenantId);
        }
    }
}
