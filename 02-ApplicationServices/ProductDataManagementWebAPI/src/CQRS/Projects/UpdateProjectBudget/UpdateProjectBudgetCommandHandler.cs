using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Models.Projects;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Projects.UpdateProjectBudget
{
    public sealed class UpdateProjectBudgetCommandHandler
        : IRequestHandler<UpdateProjectBudgetCommand, Unit>
    {
        private readonly IRepository<Project> projectRepository;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<UpdateProjectBudgetCommandHandler> logger;

        public UpdateProjectBudgetCommandHandler(
            IRepository<Project> projectRepository,
            ICurrentUser currentUser,
            ILogger<UpdateProjectBudgetCommandHandler> logger)
        {
            this.projectRepository = projectRepository;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Unit> Handle(
            UpdateProjectBudgetCommand request,
            CancellationToken cancellationToken)
        {
            Project project = await projectRepository.GetFirstBySearch(
                p => p.Id == request.ProjectId && p.TenantId == request.TenantId)
                ?? throw new NotFoundApiException(nameof(Project), request.ProjectId.ToString());

            project.BudgetNet = request.BudgetNet;
            project.BudgetGross = request.BudgetGross;

            await projectRepository.Update(project);

            logger.LogInformation(
                "Updated budget for project {ProjectId} by user {UserId}",
                project.Id, currentUser.Id);

            return Unit.Value;
        }
    }
}
