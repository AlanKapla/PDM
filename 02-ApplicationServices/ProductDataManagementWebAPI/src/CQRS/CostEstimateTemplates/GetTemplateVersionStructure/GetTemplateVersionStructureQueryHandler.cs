using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;
using Entities.Models;
using Entities.Models.CostEstimateTemplates;
using Business.Interfaces.Services;

namespace CQRS.CostEstimateTemplates.GetTemplateVersionStructure
{
    /// <summary>
    /// Handler dla pobrania pełnej struktury wersji szablonu kosztorysu
    /// </summary>
    public class GetTemplateVersionStructureQueryHandler : IRequestHandler<GetTemplateVersionStructureQuery, CostEstimateTemplateVersionStructureWeb>
    {
        private readonly IReadRepository<CostEstimateTemplateVersion> versionRepository;
        private readonly ITemplateStructureService templateStructureService;

        public GetTemplateVersionStructureQueryHandler(
            IReadRepository<CostEstimateTemplateVersion> versionRepository,
            ITemplateStructureService templateStructureService)
        {
            this.versionRepository = versionRepository;
            this.templateStructureService = templateStructureService;
        }

        public async Task<CostEstimateTemplateVersionStructureWeb> Handle(GetTemplateVersionStructureQuery request, CancellationToken cancellationToken)
        {
            // Pobierz wersję szablonu
            var version = await versionRepository.GetFirstBySearch(
                v => v.Id == request.VersionId && v.TemplateId == request.TemplateId && !v.IsDeleted,
                cancellationToken,
                q => q.Include(v => v.Template)
            );

            if (version == null)
            {
                throw new KeyNotFoundException($"Template version not found: TemplateId={request.TemplateId}, VersionId={request.VersionId}");
            }

            // Użyj wspólnego serwisu do budowania struktury
            return await templateStructureService.BuildTemplateVersionStructureAsync(
                version.Template,
                version,
                cancellationToken);
        }
    }
}
