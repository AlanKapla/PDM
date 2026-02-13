using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;
using Entities.Models.CostEstimateTemplates;
using Business.Interfaces.Services;

namespace CQRS.CostEstimateTemplates.GetCostEstimateTemplateDetails
{
    /// <summary>
    /// Handler dla pobrania szczegółów szablonu kosztorysu z pełną strukturą
    /// </summary>
    public class GetCostEstimateTemplateDetailsQueryHandler : IRequestHandler<GetCostEstimateTemplateDetailsQuery, CostEstimateTemplateDetailsWeb>
    {
        private readonly IReadRepository<CostEstimateTemplate> templateRepository;
        private readonly ITemplateStructureService templateStructureService;
        private readonly ICurrentUser currentUser;

        public GetCostEstimateTemplateDetailsQueryHandler(
            IReadRepository<CostEstimateTemplate> templateRepository,
            ITemplateStructureService templateStructureService,
            ICurrentUser currentUser)
        {
            this.templateRepository = templateRepository;
            this.templateStructureService = templateStructureService;
            this.currentUser = currentUser;
        }

        public async Task<CostEstimateTemplateDetailsWeb> Handle(GetCostEstimateTemplateDetailsQuery request, CancellationToken cancellationToken)
        {
            var template = await templateRepository.GetFirstBySearch(
                t => t.Id == request.TemplateId && !t.IsDeleted,
                cancellationToken,
                q => q.Include(t => t.Owner));

            if (template == null)
            {
                throw new NotFoundApiException(nameof(CostEstimateTemplate), request.TemplateId.ToString());
            }

            if (!currentUser.IsSuperAdmin && template.OwnerId != currentUser.Id)
            {
                throw new NotFoundApiException(nameof(CostEstimateTemplate), request.TemplateId.ToString());
            }

            var structure = await templateStructureService.BuildTemplateStructureAsync(template, cancellationToken);

            return new CostEstimateTemplateDetailsWeb(
                Id: template.Id,
                Name: template.Name,
                Description: template.Description,
                Category: template.Category,
                CanAddGroups: template.CanAddGroups,
                CanBranchGroups: template.CanBranchGroups,
                MaxGroupLevel: template.MaxGroupLevel,
                AutoNumberGroups: template.AutoNumberGroups,
                GroupNumberFormat: template.GroupNumberFormat,
                CreatedAt: template.CreatedAt,
                UpdatedAt: template.UpdatedAt,
                OwnerId: template.OwnerId,
                OwnerName: $"{template.Owner.FirstName} {template.Owner.LastName}",
                Structure: structure
            );
        }
    }
}
