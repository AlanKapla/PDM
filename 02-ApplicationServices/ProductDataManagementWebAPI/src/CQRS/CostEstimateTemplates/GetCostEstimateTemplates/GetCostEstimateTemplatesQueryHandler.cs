using Business.Interfaces.Model;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;
using Entities.Models.CostEstimateTemplates;

namespace CQRS.CostEstimateTemplates.GetCostEstimateTemplates
{
    /// <summary>
    /// Handler dla pobrania listy szablonów kosztorysów użytkownika
    /// </summary>
    public class GetCostEstimateTemplatesQueryHandler : IRequestHandler<GetCostEstimateTemplatesQuery, List<CostEstimateTemplateListItemWeb>>
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

        public async Task<List<CostEstimateTemplateListItemWeb>> Handle(GetCostEstimateTemplatesQuery request, CancellationToken cancellationToken)
        {
            var templates = await templateRepository.GetBySearch(
                t => t.OwnerId == currentUser.Id && !t.IsDeleted,
                q => q.Include(t => t.Owner));

            return templates
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new CostEstimateTemplateListItemWeb(
                    Id: t.Id,
                    Name: t.Name,
                    Description: t.Description,
                    Category: t.Category,
                    CreatedAt: t.CreatedAt,
                    UpdatedAt: t.UpdatedAt,
                    OwnerId: t.OwnerId,
                    OwnerName: $"{t.Owner.FirstName} {t.Owner.LastName}"
                ))
                .ToList();
        }
    }
}
