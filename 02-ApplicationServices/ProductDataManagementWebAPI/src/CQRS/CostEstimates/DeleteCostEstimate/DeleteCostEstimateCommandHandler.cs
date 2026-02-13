using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using MediatR;
using Repositories.Repository.Interfaces;
using Entities.Models;
using Entities.Models.CostEstimates;

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
            // 1. Verify cost estimate exists and belongs to the correct project/tenant
            var costEstimate = await costEstimateRepository.GetFirstBySearch(
                c => c.Id == request.CostEstimateId && 
                     c.TenantId == request.TenantId &&
                     c.ProjectId == request.ProjectId &&
                     !c.IsDeleted)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            // 2. Authorization check: tenant admin OR project admin OR cost estimate owner
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(request.TenantId, request.ProjectId, cancellationToken);
            bool isOwner = costEstimate.OwnerId == currentUser.Id;
            
            if (!isAdmin && !isOwner)
            {
                throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());
            }

            // 3. Soft delete
            costEstimate.IsDeleted = true;
            costEstimate.DeletedAt = DateTime.UtcNow;

            await costEstimateRepository.Update(costEstimate);
            await costEstimateRepository.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
