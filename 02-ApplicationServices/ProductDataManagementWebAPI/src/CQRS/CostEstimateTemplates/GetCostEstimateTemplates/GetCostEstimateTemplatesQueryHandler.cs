using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;
using Entities.Models;
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
            // Get all templates for current user with versions and owner
            var templates = await templateRepository.GetBySearch(
                t => t.OwnerId == currentUser.Id && !t.IsDeleted,
                q => q.Include(t => t.Versions.Where(v => !v.IsDeleted))
                      .Include(t => t.Owner));

            // Return simple list items with latest version info
            return templates
                .OrderByDescending(t => t.CreatedAt)
                .Select(t =>
                {
                    var latestVersion = t.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
                    return new CostEstimateTemplateListItemWeb(
                        Id: t.Id,
                        Name: t.Name,
                        Description: t.Description,
                        Category: t.Category,
                        CreatedAt: t.CreatedAt,
                        UpdatedAt: t.UpdatedAt,
                        LatestVersionNumber: latestVersion?.VersionNumber,
                        LatestVersionStatus: latestVersion?.Status,
                        OwnerId: t.OwnerId,
                        OwnerName: $"{t.Owner.FirstName} {t.Owner.LastName}"
                    );
                })
                .ToList();
        }
    }
}
