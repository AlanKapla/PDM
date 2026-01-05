using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositiories.Repository.Interfaces;

namespace CQRS.CostEstimates.GetCostEstimates
{
    /// <summary>
    /// Handler to get cost estimates based on scope (All, Mine, Shared)
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
            // Shared cost estimates are not implemented yet
            if (request.Scope == ResourceScope.Shared)
            {
                throw new ApiException(ApiExceptionReason.InvalidOperation, "Shared cost estimates are not yet supported");
            }

            IEnumerable<CostEstimate> costEstimates;

            switch (request.Scope)
            {
                case ResourceScope.All:
                    // Get all cost estimates in the project (requires READ_ALL permission)
                    costEstimates = await costEstimateRepository.GetBySearch(
                        c => c.ProjectId == request.ProjectId && 
                             c.TenantId == request.TenantId && 
                             !c.IsDeleted,
                        q => q.Include(c => c.Template)
                              .Include(c => c.Owner)
                              .Include(c => c.Project));
                    break;

                case ResourceScope.Mine:
                    // Get only cost estimates owned by the current user (requires READ permission)
                    costEstimates = await costEstimateRepository.GetBySearch(
                        c => c.ProjectId == request.ProjectId && 
                             c.TenantId == request.TenantId && 
                             c.OwnerId == currentUser.Id &&
                             !c.IsDeleted,
                        q => q.Include(c => c.Template)
                              .Include(c => c.Owner)
                              .Include(c => c.Project));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(request.Scope));
            }

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
