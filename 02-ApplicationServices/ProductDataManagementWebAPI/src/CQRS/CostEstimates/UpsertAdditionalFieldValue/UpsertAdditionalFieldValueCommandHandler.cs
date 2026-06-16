using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.UpsertAdditionalFieldValue
{
    public sealed class UpsertAdditionalFieldValueCommandHandler
        : IRequestHandler<UpsertAdditionalFieldValueCommand, Guid>
    {
        private readonly IRepository<CostEstimateItem> itemRepository;
        private readonly IRepository<CostEstimateFieldSchema> fieldSchemaRepository;
        private readonly IRepository<CostEstimateAdditionalFieldValue> additionalFieldValueRepository;
        private readonly ICostEstimateRecalculationService recalculationService;
        private readonly ILogger<UpsertAdditionalFieldValueCommandHandler> logger;

        public UpsertAdditionalFieldValueCommandHandler(
            IRepository<CostEstimateItem> itemRepository,
            IRepository<CostEstimateFieldSchema> fieldSchemaRepository,
            IRepository<CostEstimateAdditionalFieldValue> additionalFieldValueRepository,
            ICostEstimateRecalculationService recalculationService,
            ILogger<UpsertAdditionalFieldValueCommandHandler> logger)
        {
            this.itemRepository = itemRepository;
            this.fieldSchemaRepository = fieldSchemaRepository;
            this.additionalFieldValueRepository = additionalFieldValueRepository;
            this.recalculationService = recalculationService;
            this.logger = logger;
        }

        public async Task<Guid> Handle(
            UpsertAdditionalFieldValueCommand request,
            CancellationToken cancellationToken)
        {
            // 0. Jeśli dotyczy pozycji (ItemId), sprawdź czy nie ma wybranej opcji lub komponentów
            if (request.ItemId.HasValue)
            {
                // Wybrana opcja → blokada wszystkich pól dodatkowych
                bool hasSelectedOption = (await itemRepository.GetBySearch(
                    i => i.ParentItemId == request.ItemId.Value
                        && i.RelationType == ItemRelationType.Option
                        && i.IsSelected
                        && !i.IsDeleted)).Any();

                if (hasSelectedOption)
                {
                    throw new ValidationApiException(
                        "Cannot edit additional field values because a selected option exists. Deselect the option first.");
                }
            }

            // 1. Sprawdź czy AdditionalFieldId istnieje i należy do kosztorysu
            CostEstimateFieldSchema fieldSchema =
                await fieldSchemaRepository.GetFirstBySearch(
                    f => f.Id == request.AdditionalFieldId
                        && f.CostEstimateId == request.CostEstimateId
                        && f.IsAdditionalField)
                ?? throw new NotFoundApiException(
                    nameof(CostEstimateFieldSchema),
                    request.AdditionalFieldId.ToString());

            CostEstimateAdditionalFieldValue? existingValue =
                await additionalFieldValueRepository.GetFirstBySearch(
                    v => v.FieldSchemaId == request.AdditionalFieldId
                        && v.GroupId == request.GroupId
                        && v.ItemId == request.ItemId);

            Guid valueId;

            if (existingValue is not null)
            {
                ApplyValueToField(existingValue, fieldSchema.FieldType, request);
                existingValue.UpdatedAt = DateTime.UtcNow;
                await additionalFieldValueRepository.Update(existingValue);
                valueId = existingValue.Id;

                logger.LogInformation(
                    "Updated additional field value {ValueId} for field {FieldId}",
                    valueId,
                    request.AdditionalFieldId);
            }
            else
            {
                CostEstimateAdditionalFieldValue newValue = new CostEstimateAdditionalFieldValue
                {
                    Id = Guid.NewGuid(),
                    FieldSchemaId = request.AdditionalFieldId,
                    GroupId = request.GroupId,
                    ItemId = request.ItemId,
                    CreatedAt = DateTime.UtcNow
                };

                ApplyValueToField(newValue, fieldSchema.FieldType, request);
                await additionalFieldValueRepository.Insert(newValue);
                valueId = newValue.Id;

                logger.LogInformation(
                    "Created additional field value {ValueId} for field {FieldId}",
                    valueId,
                    request.AdditionalFieldId);
            }

            await additionalFieldValueRepository.SaveChangesAsync(cancellationToken);

            // 3. Trigger recalculation tylko jeśli zmiana dotyczy pozycji
            if (request.ItemId.HasValue)
            {
                await recalculationService.RecalculateAsync(
                    request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);
            }

            return valueId;
        }

        private static void ApplyValueToField(
            CostEstimateAdditionalFieldValue fieldValue,
            CostEstimateFieldType fieldType,
            UpsertAdditionalFieldValueCommand request)
        {
            switch (fieldType)
            {
                case CostEstimateFieldType.Text:
                    fieldValue.StringValue = request.StringValue;
                    break;

                case CostEstimateFieldType.Number:
                    fieldValue.DecimalValue = request.DecimalValue;
                    break;

                case CostEstimateFieldType.Boolean:
                    fieldValue.BoolValue = request.BoolValue;
                    break;

                case CostEstimateFieldType.Date:
                    fieldValue.DateTimeValue = request.DateTimeValue;
                    break;

                default:
                    throw new ValidationApiException(
                        $"Unsupported additional field type: {fieldType}");
            }
        }
    }
}
