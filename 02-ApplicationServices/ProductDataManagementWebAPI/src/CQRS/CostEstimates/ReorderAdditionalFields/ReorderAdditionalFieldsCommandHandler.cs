using Business.Interfaces.Exceptions;
using Entities.Models.CostEstimates;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.ReorderAdditionalFields
{
    public sealed class ReorderAdditionalFieldsCommandHandler : IRequestHandler<ReorderAdditionalFieldsCommand, Unit>
    {
        private readonly IRepository<CostEstimateFieldSchema> fieldSchemaRepository;
        private readonly ILogger<ReorderAdditionalFieldsCommandHandler> logger;

        public ReorderAdditionalFieldsCommandHandler(
            IRepository<CostEstimateFieldSchema> fieldSchemaRepository,
            ILogger<ReorderAdditionalFieldsCommandHandler> logger)
        {
            this.fieldSchemaRepository = fieldSchemaRepository;
            this.logger = logger;
        }

        public async Task<Unit> Handle(ReorderAdditionalFieldsCommand request, CancellationToken cancellationToken)
        {
            List<CostEstimateFieldSchema> allFields = (await fieldSchemaRepository.GetBySearch(
                f => f.CostEstimateId == request.CostEstimateId)).ToList();

            List<Guid> existingIds = allFields.Select(f => f.Id).ToList();
            List<Guid> missingIds = request.FieldIds.Except(existingIds).ToList();

            if (missingIds.Any())
            {
                throw new NotFoundApiException(
                    "FieldSchemas",
                    string.Join(", ", missingIds),
                    $"Następujące pola schematu nie istnieją: {string.Join(", ", missingIds)}");
            }

            Dictionary<Guid, CostEstimateFieldSchema> fieldsDict = allFields.ToDictionary(f => f.Id);
            DateTime now = DateTime.UtcNow;

            for (int i = 0; i < request.FieldIds.Count; i++)
            {
                Guid fieldId = request.FieldIds[i];
                if (fieldsDict.TryGetValue(fieldId, out CostEstimateFieldSchema? field))
                {
                    field.Order = i;
                    field.UpdatedAt = now;
                }
            }

            await fieldSchemaRepository.UpdateRange(allFields);
            await fieldSchemaRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Zmieniono kolejność {Count} pól schematu w kosztorysie {CostEstimateId}",
                request.FieldIds.Count,
                request.CostEstimateId);

            return Unit.Value;
        }
    }
}
