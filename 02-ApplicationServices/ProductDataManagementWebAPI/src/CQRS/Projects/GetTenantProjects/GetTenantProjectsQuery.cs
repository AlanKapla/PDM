using Business.Interfaces.WebModels.Projects;
using CQRS;

namespace CQRS.Projects.GetTenantProjects
{
    public record GetTenantProjectsQuery(
        Guid TenantId
    ) : IRequestQuery<IEnumerable<ProjectDetailsWeb>>;
}