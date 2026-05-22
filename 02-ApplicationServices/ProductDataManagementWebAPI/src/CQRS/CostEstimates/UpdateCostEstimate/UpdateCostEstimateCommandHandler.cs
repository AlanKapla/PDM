using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.UpdateCostEstimate
{
    public sealed class UpdateCostEstimateCommandHandler : IRequestHandler<UpdateCostEstimateCommand, Unit>
    {
        private readonly IRepository<CostEstimate> costEstimateRepository;
        private readonly ICostEstimateCacheService ceCacheService;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly ICurrentUser currentUser;

        public UpdateCostEstimateCommandHandler(
            IRepository<CostEstimate> costEstimateRepository,
            ICostEstimateCacheService ceCacheService,
            ICostEstimateAccessService ceAccessService,
            ICurrentUser currentUser)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.ceCacheService = ceCacheService;
            this.ceAccessService = ceAccessService;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(UpdateCostEstimateCommand request, CancellationToken cancellationToken)
        {
            var costEstimate = await costEstimateRepository.GetFirstBySearch(
                c => c.Id == request.CostEstimateId &&
                     c.TenantId == request.TenantId &&
                     c.ProjectId == request.ProjectId)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            var accessLevel = await ceAccessService.GetAccessLevelAsync(
                currentUser, request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            if (accessLevel != CostEstimateAccessLevel.Full)
            {
                throw new ForbiddenApiException("Only the owner or an admin can update this cost estimate.");
            }

            costEstimate.Name = request.Name;
            costEstimate.Description = request.Description;
            costEstimate.UpdatedAt = DateTime.UtcNow;

            await costEstimateRepository.Update(costEstimate);

            await ceCacheService.InvalidateCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            return Unit.Value;
        }
    }
}
