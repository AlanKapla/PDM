using Business.Implementation.Helpers;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.UpsertCostEstimateGroupField
{
    public class UpsertCostEstimateGroupFieldCommandHandler
        : IRequestHandler<UpsertCostEstimateGroupFieldCommand, Guid>
    {
        private readonly IRepository<CostEstimateGroupFieldValue> groupFieldValueRepository;
        private readonly ICostEstimateCacheService cacheService;
        private readonly ICurrentUser currentUser;

        public UpsertCostEstimateGroupFieldCommandHandler(
            IRepository<CostEstimateGroupFieldValue> groupFieldValueRepository,
            ICostEstimateCacheService cacheService,
            ICurrentUser currentUser)
        {
            this.groupFieldValueRepository = groupFieldValueRepository;
            this.cacheService = cacheService;
            this.currentUser = currentUser;
        }

        public async Task<Guid> Handle(UpsertCostEstimateGroupFieldCommand request, CancellationToken cancellationToken)
        {
            var costEstimate = await cacheService.GetCostEstimateAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, currentUser.Id, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());

            var groupsDict = await cacheService.GetGroupsDictionaryAsync(
                request.CostEstimateId, request.TenantId, request.ProjectId, cancellationToken);

            if (!groupsDict.ContainsKey(request.GroupId))
            {
                throw new NotFoundApiException(nameof(CostEstimateGroup), request.GroupId.ToString());
            }

            if (request.FieldValueId is null)
            {
                return await AddFieldValue(request, costEstimate, cancellationToken);
            }

            return await UpdateFieldValue(request, cancellationToken);
        }

        private async Task<Guid> AddFieldValue(
            UpsertCostEstimateGroupFieldCommand request,
            CostEstimate costEstimate,
            CancellationToken cancellationToken)
        {
            var template = await cacheService.GetTemplateAsync(costEstimate.TemplateId, cancellationToken)
                ?? throw new NotFoundApiException(nameof(CostEstimateTemplate), costEstimate.TemplateId.ToString());

            var fieldDef = template.GroupFieldDefinitions
                .FirstOrDefault(f => f.Id == request.FieldDefinitionId)
                ?? throw new ValidationApiException(
                    $"Field definition {request.FieldDefinitionId} not found in template group fields");

            var existing = await groupFieldValueRepository.GetFirstBySearch(
                fv => fv.GroupId == request.GroupId && fv.FieldDefinitionId == request.FieldDefinitionId);

            if (existing != null)
            {
                throw new ConflictApiException(
                    nameof(CostEstimateGroupFieldValue),
                    request.FieldDefinitionId.ToString()!,
                    $"Field value for definition {request.FieldDefinitionId} already exists on group {request.GroupId}");
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
            var fieldValue = await groupFieldValueRepository.GetFirstBySearch(
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
