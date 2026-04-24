using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using MediatR;
using Repositories.Repository.Interfaces;
using Entities.Models;
using Entities.Models.CostEstimates;

namespace CQRS.CostEstimates.DeleteCostEstimate
{
    /// <summary>
    /// Handler for soft-deleting a cost estimate and physically removing its share entries.
    /// </summary>
    public sealed class DeleteCostEstimateCommandHandler : IRequestHandler<DeleteCostEstimateCommand, Unit>
    {
        private readonly IRepository<CostEstimate> costEstimateRepository;
        private readonly IRepository<SharedCostEstimate> sharedCeRepository;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly IWorkItemLinkService workItemLinkService;
        private readonly ICurrentUser currentUser;

        public DeleteCostEstimateCommandHandler(
            IRepository<CostEstimate> costEstimateRepository,
            IRepository<SharedCostEstimate> sharedCeRepository,
            ICostEstimateAccessService ceAccessService,
            IWorkItemLinkService workItemLinkService,
            ICurrentUser currentUser)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.sharedCeRepository = sharedCeRepository;
            this.ceAccessService = ceAccessService;
            this.workItemLinkService = workItemLinkService;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(DeleteCostEstimateCommand request, CancellationToken cancellationToken)
        {
            var costEstimate = await costEstimateRepository.GetFirstBySearch(
                c => c.Id == request.CostEstimateId &&
                     c.TenantId == request.TenantId &&
                     c.ProjectId == request.ProjectId &&
                     !c.IsDeleted)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            CostEstimateAccessLevel accessLevel = await ceAccessService.GetAccessLevelAsync(
                currentUser, request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            if (accessLevel != CostEstimateAccessLevel.Full)
            {
                throw new ForbiddenApiException("Only the owner or an admin can delete this cost estimate.");
            }

            costEstimate.IsDeleted = true;
            costEstimate.DeletedAt = DateTime.UtcNow;

            await costEstimateRepository.Update(costEstimate);

            // Physically remove all share entries in a single DELETE statement
            await sharedCeRepository.ExecuteDeleteAsync(
                s => s.CostEstimateId == request.CostEstimateId,
                cancellationToken);

            await workItemLinkService.DeleteAllLinksForEstimateAsync(
                request.CostEstimateId, cancellationToken);

            await ceAccessService.InvalidateCostEstimateAccessCacheAsync(
                request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            await ceAccessService.InvalidateAccessCacheAsync(
                request.TenantId, request.ProjectId, cancellationToken);

            return Unit.Value;
        }
    }
}
