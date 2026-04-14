using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.CostEstimateTemplates.Shared;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimateTemplates.DuplicateCostEstimateTemplate
{
    public class DuplicateCostEstimateTemplateCommandHandler
        : CostEstimateTemplateHandlerBase, IRequestHandler<DuplicateCostEstimateTemplateCommand, Guid>
    {
        private readonly IRepository<CostEstimateTemplate> templateRepository;
        private readonly ICostEstimateTemplateService costEstimateTemplateService;
        private readonly ICurrentUser currentUser;

        public DuplicateCostEstimateTemplateCommandHandler(
            IRepository<CostEstimateTemplate> templateRepository,
            ICostEstimateTemplateService costEstimateTemplateService,
            ICurrentUser currentUser)
        {
            this.templateRepository = templateRepository;
            this.costEstimateTemplateService = costEstimateTemplateService;
            this.currentUser = currentUser;
        }

        public async Task<Guid> Handle(DuplicateCostEstimateTemplateCommand request, CancellationToken cancellationToken)
        {
            CostEstimateTemplate sourceTemplate = await GetAndValidateSourceTemplateAsync(request.SourceTemplateId, cancellationToken);

            ValidateRequiredTemplateFields(ExtractFieldTypes(sourceTemplate));

            return await costEstimateTemplateService.DuplicateTemplateAsync(
                sourceTemplate,
                currentUser.Id,
                request.Name,
                request.Description,
                cancellationToken);
        }

        private async Task<CostEstimateTemplate> GetAndValidateSourceTemplateAsync(Guid sourceTemplateId, CancellationToken cancellationToken)
        {
            CostEstimateTemplate? sourceTemplate = await templateRepository.GetFirstBySearch(
                t => t.Id == sourceTemplateId && t.OwnerId == currentUser.Id && !t.IsDeleted,
                q => q.Include(t => t.GroupFieldDefinitions),
                q => q.Include(t => t.SystemFieldDefinitions),
                q => q.Include(t => t.CalculatedFieldDefinitions));

            if (sourceTemplate == null)
            {
                throw new NotFoundApiException(nameof(CostEstimateTemplate), sourceTemplateId.ToString());
            }

            return sourceTemplate;
        }

        private static IEnumerable<FieldType> ExtractFieldTypes(CostEstimateTemplate template)
        {
            return template.GroupFieldDefinitions.Select(f => f.FieldType)
                .Concat(template.SystemFieldDefinitions.Select(f => f.FieldType))
                .Concat(template.CalculatedFieldDefinitions.Select(f => f.FieldType));
        }
    }
}
