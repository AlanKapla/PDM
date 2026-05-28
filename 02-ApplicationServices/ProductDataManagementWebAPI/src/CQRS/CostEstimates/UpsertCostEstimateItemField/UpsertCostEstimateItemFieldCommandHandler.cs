using Business.Implementation.Helpers;
using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Helpers;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.UpsertCostEstimateItemField
{
    public sealed class UpsertCostEstimateItemFieldCommandHandler
        : IRequestHandler<UpsertCostEstimateItemFieldCommand, Guid>
    {
        private readonly IRepository<CostEstimateItem> itemRepository;
        private readonly IRepository<CostEstimateItemFieldValue> itemFieldValueRepository;
        private readonly ICostEstimateCacheService cacheService;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly IUserService userService;
        private readonly IReadRepository<Notification> notificationRepository;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<UpsertCostEstimateItemFieldCommandHandler> logger;

        public UpsertCostEstimateItemFieldCommandHandler(
            IRepository<CostEstimateItem> itemRepository,
            IRepository<CostEstimateItemFieldValue> itemFieldValueRepository,
            ICostEstimateCacheService cacheService,
            ICostEstimateAccessService ceAccessService,
            IUserService userService,
            IReadRepository<Notification> notificationRepository,
            INotificationSender notificationSender,
            ICurrentUser currentUser,
            ILogger<UpsertCostEstimateItemFieldCommandHandler> logger)
        {
            this.itemRepository = itemRepository;
            this.itemFieldValueRepository = itemFieldValueRepository;
            this.cacheService = cacheService;
            this.ceAccessService = ceAccessService;
            this.userService = userService;
            this.notificationRepository = notificationRepository;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Guid> Handle(UpsertCostEstimateItemFieldCommand request, CancellationToken cancellationToken)
        {
            var costEstimate = await cacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            CostEstimateAccessLevel accessLevel = await ceAccessService.GetAccessLevelAsync(
                currentUser, request.TenantId, request.ProjectId, request.CostEstimateId, cancellationToken);

            if (accessLevel == CostEstimateAccessLevel.None)
                throw new ForbiddenApiException("Access to this cost estimate is not allowed.");

            if (accessLevel == CostEstimateAccessLevel.ReadOnly)
                throw new ForbiddenApiException("Read-only access does not allow field modifications.");

            var itemsDict = await cacheService.GetItemsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            if (!itemsDict.ContainsKey(request.ItemId))
                throw new NotFoundApiException(nameof(CostEstimateItem), request.ItemId.ToString());

            Guid fieldValueId = request.FieldValueId is null
                ? await AddFieldValue(request, costEstimate.TemplateId, accessLevel, cancellationToken)
                : await UpdateFieldValue(request, accessLevel, cancellationToken);

            if (costEstimate.OwnerId != currentUser.Id)
            {
                await CostEstimateFieldUpdateNotificationHelper.SendOwnerNotificationAsync(
                    request.TenantId, request.ProjectId, request.CostEstimateId,
                    costEstimate.OwnerId, currentUser, userService, notificationRepository,
                    notificationSender, logger, cancellationToken);
            }

            return fieldValueId;
        }

        private async Task<Guid> AddFieldValue(
            UpsertCostEstimateItemFieldCommand request,
            Guid templateId,
            CostEstimateAccessLevel accessLevel,
            CancellationToken cancellationToken)
        {
            CostEstimateTemplate template = await cacheService.GetTemplateAsync(templateId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimateTemplate), templateId.ToString());

            var fieldDef = template.SystemFieldDefinitions
                    .Cast<CostEstimateTemplateFieldDefinitionBase>()
                    .Concat(template.CalculatedFieldDefinitions)
                    .Concat(template.GenericFieldDefinitions)
                    .FirstOrDefault(f => f.Id == request.FieldDefinitionId)
                ?? throw new ValidationApiException(
                    $"Field definition {request.FieldDefinitionId} not found in template item fields");

            if (accessLevel == CostEstimateAccessLevel.Restricted && fieldDef.IsReadonly)
            {
                throw new ForbiddenApiException("This field is read-only and cannot be modified.");
            }

            if (fieldDef.FieldType is FieldType.ItemCalculatedVatRate
                && request.DecimalValue.HasValue
                && (request.DecimalValue.Value < 0m || request.DecimalValue.Value > 1m))
            {
                throw new ValidationApiException(
                    $"VatRate value must be between 0 and 1. Provided: {request.DecimalValue.Value}");
            }

            var itemFieldValuesDict = await cacheService.GetItemFieldValuesDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            // Jeśli pole już istnieje (np. stworzone jako domyślna wartość przy AddItem),
            // traktujemy żądanie jako update zamiast rzucać ConflictApiException.
            var existingFieldValue = itemFieldValuesDict.Values.FirstOrDefault(
                fv => fv.ItemId == request.ItemId && fv.FieldDefinitionId == request.FieldDefinitionId!.Value);

            if (existingFieldValue is not null)
            {
                FieldValueConverter.SetTypedValue(
                    existingFieldValue,
                    (int)fieldDef.FieldType,
                    request.StringValue,
                    request.DecimalValue,
                    request.BoolValue,
                    request.DateTimeValue);

                existingFieldValue.UpdatedAt = DateTime.UtcNow;

                await itemFieldValueRepository.Update(existingFieldValue);
                await itemFieldValueRepository.SaveChangesAsync(cancellationToken);

                await cacheService.InvalidateItemFieldValuesAsync(
                    request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

                await UpdateItemNameAsync(fieldDef.FieldType, request, cancellationToken);

                return existingFieldValue.Id;
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

            await UpdateItemNameAsync(fieldDef.FieldType, request, cancellationToken);

            return fieldValue.Id;
        }

        private async Task<Guid> UpdateFieldValue(
            UpsertCostEstimateItemFieldCommand request,
            CostEstimateAccessLevel accessLevel,
            CancellationToken cancellationToken)
        {
            CostEstimateItemFieldValue fieldValue = await itemFieldValueRepository.GetFirstBySearch(
                fv => fv.Id == request.FieldValueId!.Value &&
                      fv.ItemId == request.ItemId,
                q => q.Include(fv => fv.FieldDefinition))
                ?? throw new NotFoundApiException("ItemFieldValue", request.FieldValueId!.Value.ToString());

            if (accessLevel == CostEstimateAccessLevel.Restricted && fieldValue.FieldDefinition.IsReadonly)
            {
                throw new ForbiddenApiException("This field is read-only and cannot be modified.");
            }

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

            await UpdateItemNameAsync(fieldValue.FieldDefinition.FieldType, request, cancellationToken);

            return fieldValue.Id;
        }

        private async Task UpdateItemNameAsync(
            FieldType fieldType,
            UpsertCostEstimateItemFieldCommand request,
            CancellationToken cancellationToken)
        {
            if (fieldType != FieldType.ItemSystemName)
            {
                return;
            }

            CostEstimateItem item = await itemRepository.GetFirstBySearch(
                i => i.Id == request.ItemId)
                ?? throw new NotFoundApiException(nameof(CostEstimateItem), request.ItemId.ToString());

            item.Name = request.StringValue ?? string.Empty;
            item.UpdatedAt = DateTime.UtcNow;

            await itemRepository.Update(item);
            await itemRepository.SaveChangesAsync(cancellationToken);
            await cacheService.InvalidateItemsAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);
        }
    }
}
