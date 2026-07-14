using Business.Interfaces.Exceptions;
using Entities.Models.CostEstimates;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.UpdateAdditionalField
{
    public sealed class UpdateAdditionalFieldCommandHandler : IRequestHandler<UpdateAdditionalFieldCommand, Unit>
    {
        private readonly IRepository<CostEstimateFieldSchema> fieldSchemaRepository;
        private readonly ILogger<UpdateAdditionalFieldCommandHandler> logger;

        public UpdateAdditionalFieldCommandHandler(
            IRepository<CostEstimateFieldSchema> fieldSchemaRepository,
            ILogger<UpdateAdditionalFieldCommandHandler> logger)
        {
            this.fieldSchemaRepository = fieldSchemaRepository;
            this.logger = logger;
        }

        public async Task<Unit> Handle(UpdateAdditionalFieldCommand request, CancellationToken cancellationToken)
        {
            CostEstimateFieldSchema field = await fieldSchemaRepository.GetFirstBySearch(
                f => f.Id == request.FieldId && f.CostEstimateId == request.CostEstimateId)
                ?? throw new NotFoundApiException("FieldSchema", request.FieldId.ToString());

            if (request.Name is not null)
            {
                field.FieldName = request.Name;
            }

            if (request.FieldType.HasValue)
            {
                if (field.IsBasicField)
                {
                    throw new ConflictApiException(
                        nameof(CostEstimateFieldSchema),
                        request.FieldId.ToString(),
                        "Nie można zmienić typu pola podstawowego.");
                }

                field.FieldType = (CostEstimateFieldType)request.FieldType.Value;
            }

            if (request.Order.HasValue)
            {
                field.Order = request.Order.Value;
            }

            field.UpdatedAt = DateTime.UtcNow;
            await fieldSchemaRepository.Update(field);
            await fieldSchemaRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Zaktualizowano pole schematu {FieldId} w kosztorysie {CostEstimateId}",
                field.Id,
                request.CostEstimateId);

            return Unit.Value;
        }
    }
}
