using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Models;
using Entities.Models.CostEstimateData;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.UpdateCostEstimate
{
    /// <summary>
    /// Handler dla aktualizacji wypełnionego kosztorysu
    /// </summary>
    public class UpdateCostEstimateCommandHandler : IRequestHandler<UpdateCostEstimateCommand, Unit>
    {
        private readonly IRepository<CostEstimate> costEstimateRepository;
        private readonly ICurrentUser currentUser;

        public UpdateCostEstimateCommandHandler(
            IRepository<CostEstimate> costEstimateRepository,
            ICurrentUser currentUser)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(UpdateCostEstimateCommand request, CancellationToken cancellationToken)
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

            // Update metadata
            request.Data.Metadata = new CostEstimateMetadata
            {
                LastModified = DateTime.UtcNow,
                LastModifiedBy = currentUser.Id,
                SchemaVersion = request.Data.Metadata?.SchemaVersion ?? 1,
                AdditionalInfo = request.Data.Metadata?.AdditionalInfo,
                GroupCustomizations = request.Data.Metadata?.GroupCustomizations,
                WorkScopeCustomizations = request.Data.Metadata?.WorkScopeCustomizations
            };

            // Update properties - Data is automatically serialized by EF Core
            costEstimate.Name = request.Name;
            costEstimate.Description = request.Description;
            costEstimate.Status = request.Status;
            costEstimate.Data = request.Data;
            costEstimate.TotalNet = request.TotalNet;
            costEstimate.TotalGross = request.TotalGross;
            costEstimate.UpdatedAt = DateTime.UtcNow;
            costEstimate.LastCalculatedAt = request.TotalNet.HasValue || request.TotalGross.HasValue 
                ? DateTime.UtcNow 
                : costEstimate.LastCalculatedAt;

            // Save changes
            await costEstimateRepository.Update(costEstimate);
            await costEstimateRepository.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
