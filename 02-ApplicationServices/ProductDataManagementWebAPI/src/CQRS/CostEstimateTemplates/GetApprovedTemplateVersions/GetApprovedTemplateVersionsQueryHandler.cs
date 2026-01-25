using Business.Interfaces.WebModels.CostEstimateTemplates;
using Entities.Models;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimateTemplates.GetApprovedTemplateVersions
{
    /// <summary>
    /// Handler dla zapytania o wszystkie zatwierdzone wersje szablonów użytkownika
    /// Returns only approved versions from non-deleted templates
    /// </summary>
    public class GetApprovedTemplateVersionsQueryHandler 
        : IRequestHandler<GetApprovedTemplateVersionsQuery, List<ApprovedTemplateVersionItemWeb>>
    {
        private readonly IRepository<CostEstimateTemplate> templateRepository;
        private readonly IRepository<CostEstimateTemplateVersion> versionRepository;
        private readonly IRepository<User> userRepository;

        public GetApprovedTemplateVersionsQueryHandler(
            IRepository<CostEstimateTemplate> templateRepository,
            IRepository<CostEstimateTemplateVersion> versionRepository,
            IRepository<User> userRepository)
        {
            this.templateRepository = templateRepository;
            this.versionRepository = versionRepository;
            this.userRepository = userRepository;
        }

        public async Task<List<ApprovedTemplateVersionItemWeb>> Handle(
            GetApprovedTemplateVersionsQuery request, 
            CancellationToken cancellationToken)
        {
            // Get all approved versions with related data
            var approvedVersions = await versionRepository.GetBySearch(
                v => v.Status == TemplateVersionStatus.Approved && !v.IsDeleted && !v.Template.IsDeleted,
                q => q.Include(v => v.Template)
                      .Include(v => v.Currencies)
                      .Include(v => v.Units)
                      .Include(v => v.ApprovedBy));

            var result = approvedVersions
                .Select(v => new ApprovedTemplateVersionItemWeb(
                    VersionId: v.Id,
                    TemplateId: v.TemplateId,
                    TemplateName: v.Template.Name,
                    TemplateCategory: v.Template.Category,
                    VersionNumber: v.VersionNumber,
                    VersionName: v.VersionName,
                    ApprovedAt: v.ApprovedAt!.Value,
                    ApprovedByUserName: v.ApprovedBy != null 
                        ? $"{v.ApprovedBy.FirstName} {v.ApprovedBy.LastName}" 
                        : null,
                    CanAddGroups: v.CanAddGroups,
                    CanBranchGroups: v.CanBranchGroups,
                    MaxGroupLevel: v.MaxGroupLevel,
                    Currencies: v.Currencies
                        .OrderBy(c => c.Order)
                        .Select(c => new TemplateCurrencyWeb(
                            c.Id,
                            c.Code,
                            c.Name,
                            c.Symbol,
                            c.IsDefault,
                            c.Order
                        ))
                        .ToList(),
                    Units: v.Units
                        .OrderBy(u => u.Order)
                        .Select(u => new TemplateUnitWeb(
                            u.Id,
                            u.Code,
                            u.Name,
                            u.Symbol,
                            u.Category,
                            u.IsDefault,
                            u.Order
                        ))
                        .ToList()
                ))
                .OrderBy(v => v.TemplateName)
                .ThenByDescending(v => v.VersionNumber)
                .ToList();

            return result;
        }
    }
}
