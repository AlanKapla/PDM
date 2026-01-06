using Business.Interfaces.Exceptions;
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

            // 1. Verify source cost estimate exists and belongs to the correct project/tenant
            CostEstimate? sourceCostEstimate = await costEstimateRepo.GetFirstBySearch(
                ce => ce.Id == request.CostEstimateId
                    && ce.TenantId == tenantId
                    && ce.ProjectId == request.ProjectId
                    && !ce.IsDeleted)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            // 2. Authorization check: tenant admin OR project admin OR cost estimate owner
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(tenantId, request.ProjectId, cancellationToken);
            bool isOwner = sourceCostEstimate.OwnerId == currentUser.Id;
            
            if (!isAdmin && !isOwner)
            {
                throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());
            }

            List<Guid> createdCostEstimateIds = new List<Guid>();
            DateTime now = DateTime.UtcNow;

            // 3. Create copy for each target project
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
