using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositiories.Repository.Interfaces;

namespace CQRS.CostEstimates.GetCostEstimateDetails
{
    /// <summary>
    /// Handler dla pobrania szczegółów kosztorysu
    /// </summary>
    public class GetCostEstimateDetailsQueryHandler : IRequestHandler<GetCostEstimateDetailsQuery, CostEstimateDetails>
    {
        private readonly IReadRepository<CostEstimate> costEstimateRepository;
        private readonly ICurrentUser currentUser;

        public GetCostEstimateDetailsQueryHandler(
            IReadRepository<CostEstimate> costEstimateRepository,
            ICurrentUser currentUser)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.currentUser = currentUser;
        }

        public async Task<CostEstimateDetails> Handle(GetCostEstimateDetailsQuery request, CancellationToken cancellationToken)
        {
            // Get cost estimate with template and owner - filter by TenantId and ProjectId
            // Don't filter by OwnerId to allow READ_SINGLE permission (e.g., SuperAdmin access)
            var costEstimate = await costEstimateRepository.GetFirstBySearch(
                c => c.Id == request.CostEstimateId && 
                     c.TenantId == request.TenantId &&
                     c.ProjectId == request.ProjectId &&
                     !c.IsDeleted,
                cancellationToken,
                q => q.Include(c => c.Template).Include(c => c.Owner).Include(c => c.Project));

            if (costEstimate == null)
            {
                throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());
            }

            // Data is automatically deserialized by EF Core
            return new CostEstimateDetails(
                Id: costEstimate.Id,
                TenantId: costEstimate.TenantId,
                ProjectId: costEstimate.ProjectId,
                ProjectName: costEstimate.Project.Name,
                TemplateId: costEstimate.TemplateId,
                TemplateName: costEstimate.Template.Name,
                Name: costEstimate.Name,
                Description: costEstimate.Description,
                Status: costEstimate.Status,
                Data: costEstimate.Data,
                TotalNet: costEstimate.TotalNet,
                TotalGross: costEstimate.TotalGross,
                CreatedAt: costEstimate.CreatedAt,
                UpdatedAt: costEstimate.UpdatedAt,
                LastCalculatedAt: costEstimate.LastCalculatedAt,
                OwnerId: costEstimate.OwnerId,
                OwnerName: $"{costEstimate.Owner.FirstName} {costEstimate.Owner.LastName}"
            );
        }
    }
}
