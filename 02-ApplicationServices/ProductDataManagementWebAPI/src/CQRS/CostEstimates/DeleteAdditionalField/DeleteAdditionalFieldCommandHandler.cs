using Business.Interfaces.Exceptions;
using Entities.Models.CostEstimates;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.DeleteAdditionalField
{
    public sealed class DeleteAdditionalFieldCommandHandler : IRequestHandler<DeleteAdditionalFieldCommand, Unit>
    {
        private readonly IRepository<CostEstimateFieldSchema> fieldSchemaRepository;
        private readonly ILogger<DeleteAdditionalFieldCommandHandler> logger;

        public DeleteAdditionalFieldCommandHandler(
            IRepository<CostEstimateFieldSchema> fieldSchemaRepository,
            ILogger<DeleteAdditionalFieldCommandHandler> logger)
        {
            this.fieldSchemaRepository = fieldSchemaRepository;
            this.logger = logger;
        }

        public async Task<Unit> Handle(DeleteAdditionalFieldCommand request, CancellationToken cancellationToken)
        {
            CostEstimateFieldSchema field = await fieldSchemaRepository.GetFirstBySearch(
                f => f.Id == request.FieldId && f.CostEstimateId == request.CostEstimateId)
                ?? throw new NotFoundApiException("FieldSchema", request.FieldId.ToString());

            if (field.IsBasicField)
            {
                throw new ConflictApiException(
                    nameof(CostEstimateFieldSchema),
                    request.FieldId.ToString(),
                    "Nie można usunąć pola podstawowego ze schematu.");
            }

            await fieldSchemaRepository.Delete(field);

            List<CostEstimateFieldSchema> remainingFields = (await fieldSchemaRepository.GetBySearch(
                f => f.CostEstimateId == request.CostEstimateId && f.Id != request.FieldId))
                .OrderBy(f => f.Order)
                .ToList();

            for (int i = 0; i < remainingFields.Count; i++)
            {
                remainingFields[i].Order = i;
                remainingFields[i].UpdatedAt = DateTime.UtcNow;
            }

            await fieldSchemaRepository.UpdateRange(remainingFields);
            await fieldSchemaRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Usunięto pole dodatkowe {FieldId} ({Name}) z kosztorysu {CostEstimateId}",
                field.Id,
                field.FieldName,
                request.CostEstimateId);

            return Unit.Value;
        }
    }
}
