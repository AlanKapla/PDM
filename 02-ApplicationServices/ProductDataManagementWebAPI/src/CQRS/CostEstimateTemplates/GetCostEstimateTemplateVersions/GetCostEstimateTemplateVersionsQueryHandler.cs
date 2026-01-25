using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;
using Entities.Models;
using Entities.Models.CostEstimateTemplates;

namespace CQRS.CostEstimateTemplates.GetCostEstimateTemplateVersions
{
    /// <summary>
    /// Handler dla zapytania o historię wersji szablonu kosztorysu
    /// Returns only non-deleted versions (filtered by global query filter)
    /// </summary>
    public class GetCostEstimateTemplateVersionsQueryHandler 
        : IRequestHandler<GetCostEstimateTemplateVersionsQuery, List<CostEstimateTemplateVersionHistoryItemWeb>>
    {
        private readonly IRepository<CostEstimateTemplate> templateRepository;
        private readonly IRepository<CostEstimateTemplateVersion> versionRepository;
        private readonly ICurrentUser currentUser;

        public GetCostEstimateTemplateVersionsQueryHandler(
            IRepository<CostEstimateTemplate> templateRepository,
            IRepository<CostEstimateTemplateVersion> versionRepository,
            ICurrentUser currentUser)
        {
            this.templateRepository = templateRepository;
            this.versionRepository = versionRepository;
            this.currentUser = currentUser;
        }

        public async Task<List<CostEstimateTemplateVersionHistoryItemWeb>> Handle(
            GetCostEstimateTemplateVersionsQuery request, 
            CancellationToken cancellationToken)
        {
            // Verify template exists and user is owner
            var template = await templateRepository.GetFirstBySearch(
                t => t.Id == request.TemplateId && t.OwnerId == currentUser.Id && !t.IsDeleted);

            if (template == null)
            {
                throw new NotFoundApiException(nameof(CostEstimateTemplate), request.TemplateId.ToString());
            }

            // Get version history with ApprovedBy user
            var versions = await versionRepository.GetBySearch(
                v => v.TemplateId == request.TemplateId && !v.IsDeleted,
                q => q.Include(v => v.ApprovedBy));

            var result = versions
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => new CostEstimateTemplateVersionHistoryItemWeb(
                    v.Id,
                    v.TemplateId,
                    v.VersionNumber,
                    v.VersionName,
                    v.Status,
                    v.CreatedAt,
                    v.ApprovedAt,
                    v.ApprovedById,
                    v.ApprovedBy != null ? $"{v.ApprovedBy.FirstName} {v.ApprovedBy.LastName}" : null,
                    v.DeprecatedAt
                ))
                .ToList();

            return result;
        }
    }
}
