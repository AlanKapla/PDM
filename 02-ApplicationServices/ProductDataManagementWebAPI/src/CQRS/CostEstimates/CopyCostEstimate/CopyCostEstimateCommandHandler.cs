using Business.Interfaces.Model;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.CopyCostEstimate
{
    public class CopyCostEstimateCommandHandler : IRequestHandler<CopyCostEstimateCommand, List<Guid>>
    {
        private readonly IRepository<CostEstimate> costEstimateRepo;
        private readonly ICurrentUser currentUser;

        public CopyCostEstimateCommandHandler(
            IRepository<CostEstimate> costEstimateRepo,
            ICurrentUser currentUser)
        {
            this.costEstimateRepo = costEstimateRepo;
            this.currentUser = currentUser;
        }

        public async Task<List<Guid>> Handle(CopyCostEstimateCommand request, CancellationToken cancellationToken)
        {
            Guid tenantId = request.TenantId;

            // Load source cost estimate
            CostEstimate sourceCostEstimate = (await costEstimateRepo.GetFirstBySearch(
                ce => ce.Id == request.CostEstimateId
                    && ce.TenantId == tenantId
                    && ce.ProjectId == request.ProjectId
                    && !ce.IsDeleted
                    && ce.OwnerId == currentUser.Id))!;

            List<Guid> createdCostEstimateIds = new List<Guid>();
            DateTime now = DateTime.UtcNow;

            // Create copy for each target project
            foreach (Guid targetProjectId in request.TargetProjectIds)
            {
                CostEstimate copiedCostEstimate = new CostEstimate
                {
                    TenantId = tenantId,
                    ProjectId = targetProjectId,
                    TemplateId = sourceCostEstimate.TemplateId,
                    OwnerId = currentUser.Id,
                    Name = $"{sourceCostEstimate.Name} (kopia)",
                    Description = sourceCostEstimate.Description,
                    Status = CostEstimateStatus.Draft,
                    Data = sourceCostEstimate.Data,
                    TotalNet = sourceCostEstimate.TotalNet,
                    TotalGross = sourceCostEstimate.TotalGross,
                    CreatedAt = now,
                    UpdatedAt = now,
                    LastCalculatedAt = sourceCostEstimate.LastCalculatedAt,
                    IsDeleted = false
                };

                await costEstimateRepo.Insert(copiedCostEstimate);
                await costEstimateRepo.SaveChangesAsync(cancellationToken);

                createdCostEstimateIds.Add(copiedCostEstimate.Id);
            }

            return createdCostEstimateIds;
        }
    }
}
