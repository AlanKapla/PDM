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
    /// Returns full template object with structure (needed for UI)
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
            // Get cost estimate with template and owner (no Project)
            var costEstimate = await costEstimateRepository.GetFirstBySearch(
                c => c.Id == request.CostEstimateId && 
                     c.TenantId == request.TenantId &&
                     c.ProjectId == request.ProjectId &&
                     !c.IsDeleted && !c.Template.IsDeleted,
                cancellationToken,
                q => q.Include(c => c.Template)
                      .ThenInclude(t => t.Owner)
                      .Include(c => c.Owner));

            if (costEstimate == null)
            {
                throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());
            }

            return new CostEstimateDetails(
                Id: costEstimate.Id,
                TenantId: costEstimate.TenantId,
                ProjectId: costEstimate.ProjectId,
                Template: new CostEstimateTemplateDto(
                    Id: costEstimate.Template.Id,
                    Name: costEstimate.Template.Name,
                    Description: costEstimate.Template.Description,
                    TemplateStructure: costEstimate.Template.TemplateStructure,
                    CreatedAt: costEstimate.Template.CreatedAt,
                    UpdatedAt: costEstimate.Template.UpdatedAt,
                    OwnerId: costEstimate.Template.OwnerId,
                    OwnerName: $"{costEstimate.Template.Owner.FirstName} {costEstimate.Template.Owner.LastName}"
                ),
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
