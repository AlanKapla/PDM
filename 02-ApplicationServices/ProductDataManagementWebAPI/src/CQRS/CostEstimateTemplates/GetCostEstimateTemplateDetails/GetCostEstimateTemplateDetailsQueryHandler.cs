using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;
using Entities.Models;
using Entities.Models.CostEstimateTemplates;
using Business.Interfaces.Services;

namespace CQRS.CostEstimateTemplates.GetCostEstimateTemplateDetails
{
    /// <summary>
    /// Handler dla pobrania szczegółów szablonu kosztorysu
    /// Supports viewing any version from history with full structure
    /// </summary>
    public class GetCostEstimateTemplateDetailsQueryHandler : IRequestHandler<GetCostEstimateTemplateDetailsQuery, CostEstimateTemplateDetailsWeb>
    {
        private readonly IReadRepository<CostEstimateTemplate> templateRepository;
        private readonly IRepository<CostEstimateTemplateVersion> versionRepository;
        private readonly ITemplateStructureService templateStructureService;
        private readonly ICurrentUser currentUser;

        public GetCostEstimateTemplateDetailsQueryHandler(
            IReadRepository<CostEstimateTemplate> templateRepository,
            IRepository<CostEstimateTemplateVersion> versionRepository,
            ITemplateStructureService templateStructureService,
            ICurrentUser currentUser)
        {
            this.templateRepository = templateRepository;
            this.versionRepository = versionRepository;
            this.templateStructureService = templateStructureService;
            this.currentUser = currentUser;
        }

        public async Task<CostEstimateTemplateDetailsWeb> Handle(GetCostEstimateTemplateDetailsQuery request, CancellationToken cancellationToken)
        {
            // Get template with Versions and Owner
            var template = await templateRepository.GetFirstBySearch(
                t => t.Id == request.TemplateId && !t.IsDeleted,
                cancellationToken,
                q => q.Include(t => t.Versions.Where(v => !v.IsDeleted))
                          .ThenInclude(v => v.ApprovedBy)
                      .Include(t => t.Owner));

            if (template == null)
            {
                throw new NotFoundApiException(nameof(CostEstimateTemplate), request.TemplateId.ToString());
            }

            // Additional ownership check for non-SuperAdmin users
            if (!currentUser.IsSuperAdmin && template.OwnerId != currentUser.Id)
            {
                throw new NotFoundApiException(nameof(CostEstimateTemplate), request.TemplateId.ToString());
            }

            // Determine which version to return
            CostEstimateTemplateVersion? versionToShow;
            
            if (request.VersionId.HasValue)
            {
                // Load specific version from history
                var versionWithStructure = await versionRepository.GetFirstBySearch(
                    v => v.Id == request.VersionId.Value && v.TemplateId == request.TemplateId && !v.IsDeleted,
                    q => q.Include(v => v.ApprovedBy)
                );

                if (versionWithStructure == null)
                {
                    throw new NotFoundApiException(
                        nameof(CostEstimateTemplateVersion), 
                        request.VersionId.Value.ToString());
                }

                versionToShow = versionWithStructure;
            }
            else
            {
                // Get latest version
                var latestVersion = template.Versions
                    .OrderByDescending(v => v.VersionNumber)
                    .FirstOrDefault();

                if (latestVersion != null)
                {
                    versionToShow = await versionRepository.GetFirstBySearch(
                        v => v.Id == latestVersion.Id,
                        q => q.Include(v => v.ApprovedBy)
                    );
                }
                else
                {
                    versionToShow = null;
                }
            }

            CostEstimateTemplateVersionInfoWeb? selectedVersionInfo = null;
            CostEstimateTemplateVersionStructureWeb? versionStructure = null;

            if (versionToShow != null)
            {
                selectedVersionInfo = new CostEstimateTemplateVersionInfoWeb(
                    Id: versionToShow.Id,
                    VersionNumber: versionToShow.VersionNumber,
                    VersionName: versionToShow.VersionName,
                    Status: versionToShow.Status,
                    CreatedAt: versionToShow.CreatedAt,
                    ApprovedAt: versionToShow.ApprovedAt,
                    ApprovedById: versionToShow.ApprovedById,
                    ApprovedByUserName: versionToShow.ApprovedBy != null 
                        ? $"{versionToShow.ApprovedBy.FirstName} {versionToShow.ApprovedBy.LastName}" 
                        : null,
                    DeprecatedAt: versionToShow.DeprecatedAt
                );

                // Build full version structure przez wspólny serwis
                versionStructure = await templateStructureService.BuildTemplateVersionStructureAsync(
                    template, 
                    versionToShow, 
                    cancellationToken);
            }

            return new CostEstimateTemplateDetailsWeb(
                Id: template.Id,
                Name: template.Name,
                Description: template.Description,
                Category: template.Category,
                CanAddGroups: versionToShow?.CanAddGroups ?? true,
                CanBranchGroups: versionToShow?.CanBranchGroups ?? true,
                MaxGroupLevel: versionToShow?.MaxGroupLevel,
                AutoNumberGroups: versionToShow?.AutoNumberGroups ?? false,
                GroupNumberFormat: versionToShow?.GroupNumberFormat,
                CreatedAt: template.CreatedAt,
                UpdatedAt: template.UpdatedAt,
                OwnerId: template.OwnerId,
                OwnerName: $"{template.Owner.FirstName} {template.Owner.LastName}",
                SelectedVersion: selectedVersionInfo,
                VersionStructure: versionStructure
            );
        }
    }
}
