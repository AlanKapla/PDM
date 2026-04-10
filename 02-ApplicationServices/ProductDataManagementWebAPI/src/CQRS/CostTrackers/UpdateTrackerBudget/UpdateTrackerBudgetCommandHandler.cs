using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.CostTrackers.Shared;
using Entities.Models.CostTrackers;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.CostTrackers.UpdateTrackerBudget
{
    public sealed class UpdateTrackerBudgetCommandHandler
        : CostTrackerHandlerBase, IRequestHandler<UpdateTrackerBudgetCommand, Unit>
    {
        private readonly ILogger<UpdateTrackerBudgetCommandHandler> logger;

        public UpdateTrackerBudgetCommandHandler(
            IReadRepository<CostTracker> trackerRepository,
            IReadRepository<TrackedCost> trackedCostRepository,
            ICurrentUser currentUser,
            ILogger<UpdateTrackerBudgetCommandHandler> logger)
            : base(trackerRepository, currentUser, trackedCostRepository)
        {
            this.logger = logger;
        }

        public async Task<Unit> Handle(
            UpdateTrackerBudgetCommand request,
            CancellationToken cancellationToken)
        {
            CostTracker tracker = await GetAndValidateTrackerAsync(
                request.CostTrackerId, request.TenantId, request.ProjectId, cancellationToken);

            tracker.BudgetNet = request.BudgetNet;
            tracker.BudgetGross = request.BudgetGross;

            await trackerRepository.Update(tracker);

            logger.LogInformation(
                "Updated budget for CostTracker {TrackerId} by user {UserId}",
                tracker.Id, currentUser.Id);

            return Unit.Value;
        }

        private async Task<CostTracker> GetAndValidateTrackerAsync(
            Guid costTrackerId, Guid tenantId, Guid projectId, CancellationToken cancellationToken)
        {
            await ValidateAccessAsync(tenantId, projectId, cancellationToken);

            return await trackerRepository.GetFirstBySearch(
                t => t.Id == costTrackerId && t.TenantId == tenantId && t.ProjectId == projectId,
                cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostTracker), costTrackerId.ToString());
        }
    }
}
