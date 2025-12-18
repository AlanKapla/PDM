using Business.Interfaces.Model;
using CQRS.CostEstimates.GetCostEstimateTemplates;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositiories.Repository.Interfaces;

namespace CQRS.CostEstimates.GetCostEstimateTemplates
{
    /// <summary>
    /// Handler dla pobrania listy szablonów kosztorysów użytkownika
    /// </summary>
    public class GetCostEstimateTemplatesQueryHandler : IRequestHandler<GetCostEstimateTemplatesQuery, List<CostEstimateTemplateListItem>>
    {
        private readonly IReadRepository<CostEstimateTemplate> templateRepository;
        private readonly ICurrentUser currentUser;

        public GetCostEstimateTemplatesQueryHandler(
            IReadRepository<CostEstimateTemplate> templateRepository,
            ICurrentUser currentUser)
        {
            this.templateRepository = templateRepository;
            this.currentUser = currentUser;
        }

        public async Task<List<CostEstimateTemplateListItem>> Handle(GetCostEstimateTemplatesQuery request, CancellationToken cancellationToken)
        {
            // Get all templates for current user
            var templates = await templateRepository.GetBySearch(
                t => t.OwnerId == currentUser.Id && !t.IsDeleted,
                q => q.Include(t => t.Owner));

            // Return simple list items
            return templates
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new CostEstimateTemplateListItem(
                    Id: t.Id,
                    Name: t.Name,
                    Description: t.Description,
                    CreatedAt: t.CreatedAt,
                    UpdatedAt: t.UpdatedAt,
                    OwnerId: t.OwnerId,
                    OwnerName: $"{t.Owner.FirstName} {t.Owner.LastName}"
                ))
                .ToList();
        }
    }
}
