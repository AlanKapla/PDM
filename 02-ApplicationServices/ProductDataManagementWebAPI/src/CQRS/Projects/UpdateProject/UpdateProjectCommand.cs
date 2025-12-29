using Business.Interfaces.WebModels.Projects;

namespace CQRS.Projects.UpdateProject
{
    public sealed record UpdateProjectCommand(
        Guid TenantId,
        Guid ProjectId,
        string Name
    ) : IRequestCommand<ProjectDetailsWeb>;
}
