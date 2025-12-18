using Business.Interfaces.Model;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositiories.Repository.Interfaces;

namespace CQRS.CostEstimates.GetCostEstimates
{
    /// <summary>
    /// Handler dla pobrania listy kosztorysów dla projektu
    /// </summary>
    public class GetCostEstimatesQueryHandler : IRequestHandler<GetCostEstimatesQuery, List<CostEstimateListItem>>
    {
        private readonly IReadRepository<CostEstimate> costEstimateRepository;
        private readonly ICurrentUser currentUser;

        public GetCostEstimatesQueryHandler(
            IReadRepository<CostEstimate> costEstimateRepository,
            ICurrentUser currentUser)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.currentUser = currentUser;
        }

        public async Task<List<CostEstimateListItem>> Handle(GetCostEstimatesQuery request, CancellationToken cancellationToken)
        {
            // Get all cost estimates for project - filter by TenantId, ProjectId and OwnerId
            var costEstimates = await costEstimateRepository.GetBySearch(
                c => c.ProjectId == request.ProjectId && 
                     c.TenantId == request.TenantId && 
                     c.OwnerId == currentUser.Id &&
                     !c.IsDeleted,
                q => q.Include(c => c.Template).Include(c => c.Owner).Include(c => c.Project));

            // Return simple list items
            return costEstimates
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CostEstimateListItem(
                    Id: c.Id,
                    TenantId: c.TenantId,
                    ProjectId: c.ProjectId,
                    ProjectName: c.Project.Name,
                    TemplateId: c.TemplateId,
                    TemplateName: c.Template.Name,
                    Name: c.Name,
                    Description: c.Description,
                    Status: c.Status,
                    TotalNet: c.TotalNet,
                    TotalGross: c.TotalGross,
                    CreatedAt: c.CreatedAt,
                    UpdatedAt: c.UpdatedAt,
                    OwnerId: c.OwnerId,
                    OwnerName: $"{c.Owner.FirstName} {c.Owner.LastName}"
                ))
                .ToList();
        }
    }
}
