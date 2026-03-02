using Business.Implementation.Helpers;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.UpsertCostEstimateItemField
{
    public class UpsertCostEstimateItemFieldCommandHandler
        : IRequestHandler<UpsertCostEstimateItemFieldCommand, Guid>
    {
        private readonly IRepository<CostEstimateItemFieldValue> itemFieldValueRepository;
        private readonly ICostEstimateCacheService cacheService;
        private readonly ICurrentUser currentUser;

        public UpsertCostEstimateItemFieldCommandHandler(
            IRepository<CostEstimateItemFieldValue> itemFieldValueRepository,
            ICostEstimateCacheService cacheService,
            ICurrentUser currentUser)
        {
            this.itemFieldValueRepository = itemFieldValueRepository;
            this.cacheService = cacheService;
            this.currentUser = currentUser;
        }

        public async Task<Guid> Handle(UpsertCostEstimateItemFieldCommand request, CancellationToken cancellationToken)
        {
            var costEstimate = await cacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, currentUser.Id, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            var itemsDict = await cacheService.GetItemsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            if (!itemsDict.ContainsKey(request.ItemId))
            {
                throw new NotFoundApiException(nameof(CostEstimateItem), request.ItemId.ToString());
            }

            if (request.FieldValueId is null)
            {
                return await AddFieldValue(request, costEstimate, cancellationToken);
            }

            return await UpdateFieldValue(request, cancellationToken);
        }

        private async Task<Guid> AddFieldValue(
            UpsertCostEstimateItemFieldCommand request,
            CostEstimate costEstimate,
            CancellationToken cancellationToken)
        {
            var template = await cacheService.GetTemplateAsync(costEstimate.TemplateId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimateTemplate), costEstimate.TemplateId.ToString());

            var fieldDef = template.SystemFieldDefinitions
                    .Cast<CostEstimateTemplateFieldDefinitionBase>()
                    .Concat(template.CalculatedFieldDefinitions)
                    .Concat(template.GenericFieldDefinitions)
                    .FirstOrDefault(f => f.Id == request.FieldDefinitionId)
                ?? throw new ValidationApiException(
                    $"Field definition {request.FieldDefinitionId} not found in template item fields");

            if (fieldDef.FieldType is FieldType.ItemCalculatedVatRate
                && request.DecimalValue.HasValue
                && (request.DecimalValue.Value < 0m || request.DecimalValue.Value > 1m))
            {
                throw new ValidationApiException(
                    $"VatRate value must be between 0 and 1. Provided: {request.DecimalValue.Value}");
            }

            var existing = await itemFieldValueRepository.GetFirstBySearch(
                fv => fv.ItemId == request.ItemId && fv.FieldDefinitionId == request.FieldDefinitionId);

            if (existing != null)
            {
                throw new ConflictApiException(
                    nameof(CostEstimateItemFieldValue),
                    request.FieldDefinitionId.ToString()!,
                    $"Field value for definition {request.FieldDefinitionId} already exists on item {request.ItemId}");
            }

            var fieldValue = new CostEstimateItemFieldValue
            {
                ItemId = request.ItemId,
                FieldDefinitionId = request.FieldDefinitionId!.Value,
                CreatedAt = DateTime.UtcNow
            };

            FieldValueConverter.SetTypedValue(
                fieldValue,
                (int)fieldDef.FieldType,
                request.StringValue,
                request.DecimalValue,
                request.BoolValue,
                request.DateTimeValue);

            await itemFieldValueRepository.Insert(fieldValue);
            await itemFieldValueRepository.SaveChangesAsync(cancellationToken);

            await cacheService.InvalidateItemFieldValuesAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            return fieldValue.Id;
        }

        private async Task<Guid> UpdateFieldValue(
            UpsertCostEstimateItemFieldCommand request,
            CancellationToken cancellationToken)
        {
            var fieldValue = await itemFieldValueRepository.GetFirstBySearch(
                fv => fv.Id == request.FieldValueId!.Value &&
                      fv.ItemId == request.ItemId,
                q => q.Include(fv => fv.FieldDefinition))
                ?? throw new NotFoundApiException("ItemFieldValue", request.FieldValueId!.Value.ToString());

            FieldValueConverter.SetTypedValue(
                fieldValue,
                (int)fieldValue.FieldDefinition.FieldType,
                request.StringValue,
                request.DecimalValue,
                request.BoolValue,
                request.DateTimeValue);

            fieldValue.UpdatedAt = DateTime.UtcNow;

            await itemFieldValueRepository.Update(fieldValue);
            await itemFieldValueRepository.SaveChangesAsync(cancellationToken);

            await cacheService.InvalidateItemFieldValuesAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            return fieldValue.Id;
        }
    }
}
