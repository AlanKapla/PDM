using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.DeleteCostEstimate
{
    /// <summary>
    /// Handler dla usunięcia kosztorysu (soft delete)
    /// </summary>
    public class DeleteCostEstimateCommandHandler : IRequestHandler<DeleteCostEstimateCommand, Unit>
    {
        private readonly IRepository<CostEstimate> costEstimateRepository;
        private readonly ICurrentUser currentUser;

        public DeleteCostEstimateCommandHandler(
            IRepository<CostEstimate> costEstimateRepository,
            ICurrentUser currentUser)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(DeleteCostEstimateCommand request, CancellationToken cancellationToken)
        {
            // Get existing cost estimate - filter by TenantId, ProjectId and OwnerId
            var costEstimate = await costEstimateRepository.GetFirstBySearch(
                c => c.Id == request.CostEstimateId && 
                     c.TenantId == request.TenantId &&
                     c.ProjectId == request.ProjectId &&
                     c.OwnerId == currentUser.Id &&
                     !c.IsDeleted);

            if (costEstimate == null)
            {
                throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());
            }

            // Soft delete
            costEstimate.IsDeleted = true;
            costEstimate.DeletedAt = DateTime.UtcNow;

            // Save changes
            await costEstimateRepository.Update(costEstimate);
            await costEstimateRepository.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
