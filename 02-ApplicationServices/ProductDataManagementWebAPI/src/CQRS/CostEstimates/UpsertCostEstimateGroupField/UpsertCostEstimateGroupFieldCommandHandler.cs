using Business.Implementation.Helpers;
using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Helpers;
using Entities.Models;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.UpsertCostEstimateGroupField
{
    public sealed class UpsertCostEstimateGroupFieldCommandHandler
        : IRequestHandler<UpsertCostEstimateGroupFieldCommand, Guid>
    {
        private readonly IRepository<CostEstimateGroupFieldValue> groupFieldValueRepository;
        private readonly ICostEstimateCacheService cacheService;
        private readonly ICostEstimateAccessService ceAccessService;
        private readonly IUserService userService;
        private readonly IReadRepository<Notification> notificationRepository;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;
        private readonly ILogger<UpsertCostEstimateGroupFieldCommandHandler> logger;

        public UpsertCostEstimateGroupFieldCommandHandler(
            IRepository<CostEstimateGroupFieldValue> groupFieldValueRepository,
            ICostEstimateCacheService cacheService,
            ICostEstimateAccessService ceAccessService,
            IUserService userService,
            IReadRepository<Notification> notificationRepository,
            INotificationSender notificationSender,
            ICurrentUser currentUser,
            ILogger<UpsertCostEstimateGroupFieldCommandHandler> logger)
        {
            this.groupFieldValueRepository = groupFieldValueRepository;
            this.cacheService = cacheService;
            this.ceAccessService = ceAccessService;
            this.userService = userService;
            this.notificationRepository = notificationRepository;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
            this.logger = logger;
        }

        public async Task<Guid> Handle(UpsertCostEstimateGroupFieldCommand request, CancellationToken cancellationToken)
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

            // Fetch template once — needed for Add path (field def lookup) and Restricted read-only check
            CostEstimateTemplate? template = null;
            if (request.FieldValueId is null || (accessLevel == CostEstimateAccessLevel.Restricted && request.FieldDefinitionId.HasValue))
            {
                template = await cacheService.GetTemplateAsync(costEstimate.TemplateId, cancellationToken)
                    ?? throw new NotFoundApiException(nameof(CostEstimateTemplate), costEstimate.TemplateId.ToString());
            }

            if (accessLevel == CostEstimateAccessLevel.Restricted && request.FieldDefinitionId.HasValue)
            {
                var fieldDef = template!.GroupFieldDefinitions
                    .Cast<CostEstimateTemplateFieldDefinitionBase>()
                    .FirstOrDefault(f => f.Id == request.FieldDefinitionId.Value);

                if (fieldDef?.IsReadonly == true)
                    throw new ForbiddenApiException("This field is read-only and cannot be modified.");
            }

            var groupsDict = await cacheService.GetGroupsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            if (!groupsDict.ContainsKey(request.GroupId))
                throw new NotFoundApiException(nameof(CostEstimateGroup), request.GroupId.ToString());

            Guid fieldValueId = request.FieldValueId is null
                ? await AddFieldValue(request, template!, cancellationToken)
                : await UpdateFieldValue(request, cancellationToken);

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
            UpsertCostEstimateGroupFieldCommand request,
            CostEstimateTemplate template,
            CancellationToken cancellationToken)
        {
            var fieldDef = template.GroupFieldDefinitions
                .FirstOrDefault(f => f.Id == request.FieldDefinitionId)
                ?? throw new ValidationApiException(
                    $"Field definition {request.FieldDefinitionId} not found in template group fields");

            var groupFieldValuesDict = await cacheService.GetGroupFieldValuesDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            // Jeśli pole już istnieje (np. stworzone jako domyślna wartość przy AddGroup),
            // traktujemy żądanie jako update zamiast rzucać ConflictApiException.
            var existingFieldValue = groupFieldValuesDict.Values.FirstOrDefault(
                fv => fv.GroupId == request.GroupId && fv.FieldDefinitionId == request.FieldDefinitionId!.Value);

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

                await groupFieldValueRepository.Update(existingFieldValue);
                await groupFieldValueRepository.SaveChangesAsync(cancellationToken);

                await cacheService.InvalidateGroupFieldValuesAsync(
                    request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

                return existingFieldValue.Id;
            }

            var fieldValue = new CostEstimateGroupFieldValue
            {
                GroupId = request.GroupId,
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

            await groupFieldValueRepository.Insert(fieldValue);
            await groupFieldValueRepository.SaveChangesAsync(cancellationToken);

            await cacheService.InvalidateGroupFieldValuesAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            return fieldValue.Id;
        }

        private async Task<Guid> UpdateFieldValue(
            UpsertCostEstimateGroupFieldCommand request,
            CancellationToken cancellationToken)
        {
            CostEstimateGroupFieldValue fieldValue = await groupFieldValueRepository.GetFirstBySearch(
                fv => fv.Id == request.FieldValueId!.Value &&
                      fv.GroupId == request.GroupId,
                q => q.Include(fv => fv.FieldDefinition))
                ?? throw new NotFoundApiException("GroupFieldValue", request.FieldValueId!.Value.ToString());

            FieldValueConverter.SetTypedValue(
                fieldValue,
                (int)fieldValue.FieldDefinition.FieldType,
                request.StringValue,
                request.DecimalValue,
                request.BoolValue,
                request.DateTimeValue);

            fieldValue.UpdatedAt = DateTime.UtcNow;

            await groupFieldValueRepository.Update(fieldValue);
            await groupFieldValueRepository.SaveChangesAsync(cancellationToken);

            await cacheService.InvalidateGroupFieldValuesAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            return fieldValue.Id;
        }
    }
}
