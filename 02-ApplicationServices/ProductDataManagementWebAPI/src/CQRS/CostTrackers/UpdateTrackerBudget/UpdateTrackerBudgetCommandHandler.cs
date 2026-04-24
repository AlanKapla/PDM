using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.CostTrackers.Shared;
using Entities.Models;
using Entities.Models.CostTrackers;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.CostTrackers.UpdateTrackerBudget
{
    public sealed class UpdateTrackerBudgetCommandHandler
        : CostTrackerHandlerBase, IRequestHandler<UpdateTrackerBudgetCommand, Unit>
    {
        private readonly IReadRepository<Project> projectRepository;
        private readonly ILogger<UpdateTrackerBudgetCommandHandler> logger;

        public UpdateTrackerBudgetCommandHandler(
            IReadRepository<TrackedCost> trackedCostRepository,
            IReadRepository<Project> projectRepository,
            ICurrentUser currentUser,
            ILogger<UpdateTrackerBudgetCommandHandler> logger)
            : base(currentUser, trackedCostRepository)
        {
            this.projectRepository = projectRepository;
            this.logger = logger;
        }

        public async Task<Unit> Handle(
            UpdateTrackerBudgetCommand request,
            CancellationToken cancellationToken)
        {
            await ValidateAccessAsync(request.TenantId, request.ProjectId, cancellationToken);

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
