using Business.Interfaces.WebModels.Projects;

namespace CQRS.Projects.CreateProject
{
    public record CreateProjectCommand(string Name) : IRequestCommand<ProjectDetailsWeb>;
}
