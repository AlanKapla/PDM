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
    /// Only returns cost estimates where template is NOT deleted
    /// </summary>
    public class GetCostEstimatesQueryHandler : IRequestHandler<GetCostEstimatesQuery, List<CostEstimateListItem>>
    {
        private readonly IReadRepository<CostEstimate> costEstimateRepository;
        private readonly IReadRepository<CostEstimateTemplate> templateRepository;
        private readonly ICurrentUser currentUser;

        public GetCostEstimatesQueryHandler(
            IReadRepository<CostEstimate> costEstimateRepository,
            IReadRepository<CostEstimateTemplate> templateRepository,
            ICurrentUser currentUser)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.templateRepository = templateRepository;
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
                    // Get all cost estimates in the project where template is NOT deleted
                    costEstimates = await costEstimateRepository.GetBySearch(
                        c => c.ProjectId == request.ProjectId && 
                             c.TenantId == request.TenantId && 
                             !c.IsDeleted &&
                             !c.Template.IsDeleted,  // Only non-deleted templates
                        q => q.Include(c => c.Owner));
                    break;

                case ResourceScope.Mine:
                    // Get only cost estimates owned by the current user where template is NOT deleted
                    costEstimates = await costEstimateRepository.GetBySearch(
                        c => c.ProjectId == request.ProjectId && 
                             c.TenantId == request.TenantId && 
                             c.OwnerId == currentUser.Id &&
                             !c.IsDeleted &&
                             !c.Template.IsDeleted,  // Only non-deleted templates
                        q => q.Include(c => c.Owner));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(request.Scope));
            }

            var costEstimatesList = costEstimates.ToList();

            if (!costEstimatesList.Any())
            {
                return new List<CostEstimateListItem>();
            }

            // Get unique template IDs from cost estimates
            var templateIds = costEstimatesList
                .Select(c => c.TemplateId)
                .Distinct()
                .ToList();

            // Fetch templates in separate query (only non-deleted due to Global Query Filter)
            var templates = await templateRepository.GetBySearch(
                t => templateIds.Contains(t.Id));

            // Create dictionary for fast lookup
            var templateDict = templates.ToDictionary(t => t.Id, t => t.Name);

            // Map to list items
            return costEstimatesList
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CostEstimateListItem(
                    Id: c.Id,
                    TenantId: c.TenantId,
                    ProjectId: c.ProjectId,
                    TemplateId: c.TemplateId,
                    TemplateName: templateDict.GetValueOrDefault(c.TemplateId, "Unknown Template"),
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
