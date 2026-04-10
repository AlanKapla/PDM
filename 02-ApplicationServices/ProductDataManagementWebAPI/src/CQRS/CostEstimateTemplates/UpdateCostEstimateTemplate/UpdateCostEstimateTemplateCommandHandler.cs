using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using CQRS.CostEstimateTemplates.Shared;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimateTemplates.UpdateCostEstimateTemplate
{
    public class UpdateCostEstimateTemplateCommandHandler : CostEstimateTemplateHandlerBase, IRequestHandler<UpdateCostEstimateTemplateCommand, Unit>
    {
        private readonly IRepository<CostEstimateTemplate> templateRepository;
        private readonly ICostEstimateTemplateService templateService;
        private readonly ICurrentUser currentUser;

        public UpdateCostEstimateTemplateCommandHandler(
            IRepository<CostEstimateTemplate> templateRepository,
            ICostEstimateTemplateService templateService,
            ICurrentUser currentUser)
        {
            this.templateRepository = templateRepository;
            this.templateService = templateService;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(UpdateCostEstimateTemplateCommand request, CancellationToken cancellationToken)
        {
            CostEstimateTemplate template = await GetAndValidateTemplateAsync(request.TemplateId);

            if (request.UpdateStructure)
            {
                ValidateRequiredTemplateFields(ExtractFieldTypes(
                    request.GroupHeaderFields,
                    request.SystemFields,
                    request.CalculatedFields));
            }

            await templateService.UpdateTemplateAsync(
                template,
                request.Name,
                request.Description,
                request.Category,
                request.CanAddGroups,
                request.CanBranchGroups,
                request.MaxGroupLevel,
                request.AutoNumberGroups,
                request.GroupNumberFormat,
                request.UpdateStructure,
                request.Currencies,
                request.Units,
                request.Categories,
                request.GroupHeaderFields,
                request.SystemFields,
                request.CalculatedFields,
                request.GenericFields,
                request.UiConfiguration,
                cancellationToken);

            return Unit.Value;
        }

        private async Task<CostEstimateTemplate> GetAndValidateTemplateAsync(Guid templateId)
        {
            CostEstimateTemplate? template = await templateRepository.GetFirstBySearch(
                t => t.Id == templateId && t.OwnerId == currentUser.Id && !t.IsDeleted);

            if (template == null)
            {
                throw new NotFoundApiException(nameof(CostEstimateTemplate), templateId.ToString());
            }

            return template;
        }

        private static IEnumerable<FieldType> ExtractFieldTypes(
            List<FieldDefinitionDto>? groupHeaderFields,
            List<FieldDefinitionDto>? systemFields,
            List<FieldDefinitionDto>? calculatedFields)
        {
            return (groupHeaderFields ?? [])
                .Concat(systemFields ?? [])
                .Concat(calculatedFields ?? [])
                .Select(f => (FieldType)f.FieldType);
        }
    }
}
