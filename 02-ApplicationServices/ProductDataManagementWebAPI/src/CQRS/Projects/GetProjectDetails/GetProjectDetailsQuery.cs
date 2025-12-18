using Business.Interfaces.WebModels.Projects;
using MediatR;

namespace CQRS.Projects.GetProjectDetails
{
    public record GetProjectDetailsQuery(Guid TenantId, Guid ProjectId) : IRequest<ProjectDetailsWeb>;
}
