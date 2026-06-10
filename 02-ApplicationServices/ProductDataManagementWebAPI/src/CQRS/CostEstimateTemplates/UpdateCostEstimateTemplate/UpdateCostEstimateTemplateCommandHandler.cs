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
                // Automatycznie wymuś że GroupName (FieldType=0) jest pierwszy w GroupColumnLayout, a ItemSystemName (FieldType=100) pierwszy w ItemColumnLayout
                EnforceRequiredFieldLayoutOrder(request.GroupHeaderFields, request.SystemFields, request.UiConfiguration);

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

        /// <summary>
        /// Wymusza, że GroupName (FieldType=0) jest pierwszy w GroupColumnLayout, a ItemSystemName (FieldType=100) pierwszy w ItemColumnLayout.
        /// Automatycznie przesuwa GUID tych pól na początek odpowiednich list layoutu.
        /// </summary>
        private static void EnforceRequiredFieldLayoutOrder(
            List<FieldDefinitionDto>? groupHeaderFields,
            List<FieldDefinitionDto>? systemFields,
            UiConfigurationDto? uiConfiguration)
        {
            if (uiConfiguration == null)
            {
                return;
            }

            // GroupColumnLayout — wymuś GroupName (FieldType=0) jako pierwszy
            if (uiConfiguration.GroupColumnLayout != null && groupHeaderFields != null)
            {
                Guid? groupNameGuid = groupHeaderFields
                    .FirstOrDefault(f => f.FieldType == (int)FieldType.GroupName)
                    ?.FieldName;

                if (groupNameGuid.HasValue && groupNameGuid.Value != Guid.Empty)
                {
                    List<Guid> layout = uiConfiguration.GroupColumnLayout;
                    int currentIndex = layout.IndexOf(groupNameGuid.Value);
                    if (currentIndex > 0)
                    {
                        layout.RemoveAt(currentIndex);
                        layout.Insert(0, groupNameGuid.Value);
                    }
                }
            }

            // ItemColumnLayout — wymuś ItemSystemName (FieldType=100) jako pierwszy
            if (uiConfiguration.ItemColumnLayout != null && systemFields != null)
            {
                Guid? itemSystemNameGuid = systemFields
                    .FirstOrDefault(f => f.FieldType == (int)FieldType.ItemSystemName)
                    ?.FieldName;

                if (itemSystemNameGuid.HasValue && itemSystemNameGuid.Value != Guid.Empty)
                {
                    List<Guid> layout = uiConfiguration.ItemColumnLayout;
                    int currentIndex = layout.IndexOf(itemSystemNameGuid.Value);
                    if (currentIndex > 0)
                    {
                        layout.RemoveAt(currentIndex);
                        layout.Insert(0, itemSystemNameGuid.Value);
                    }
                }
            }
        }
    }
}
