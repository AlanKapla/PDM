using Business.Interfaces.Exceptions;
using Entities.Models.CostEstimates;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.AddAdditionalField
{
    public sealed class AddAdditionalFieldCommandHandler : IRequestHandler<AddAdditionalFieldCommand, Guid>
    {
        private readonly IReadRepository<CostEstimate> costEstimateRepository;
        private readonly IRepository<CostEstimateFieldSchema> fieldSchemaRepository;
        private readonly ILogger<AddAdditionalFieldCommandHandler> logger;

        public AddAdditionalFieldCommandHandler(
            IReadRepository<CostEstimate> costEstimateRepository,
            IRepository<CostEstimateFieldSchema> fieldSchemaRepository,
            ILogger<AddAdditionalFieldCommandHandler> logger)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.fieldSchemaRepository = fieldSchemaRepository;
            this.logger = logger;
        }

        public async Task<Guid> Handle(AddAdditionalFieldCommand request, CancellationToken cancellationToken)
        {
            bool costEstimateExists = await costEstimateRepository.AnyAsync(
                ce => ce.Id == request.CostEstimateId
                    && ce.TenantId == request.TenantId
                    && ce.ProjectId == request.ProjectId,
                cancellationToken);

            if (!costEstimateExists)
            {
                throw new NotFoundApiException(nameof(CostEstimate), request.CostEstimateId.ToString());
            }

            int order;
            if (request.Order.HasValue)
            {
                order = request.Order.Value;
            }
            else
            {
                List<int> existingOrders = await fieldSchemaRepository.SelectAsync(
                    f => f.CostEstimateId == request.CostEstimateId,
                    f => f.Order,
                    cancellationToken);

                order = existingOrders.Count > 0 ? existingOrders.Max() + 1 : 0;
            }

            Guid fieldId = Guid.NewGuid();
            CostEstimateFieldSchema field = new()
            {
                Id = fieldId,
                CostEstimateId = request.CostEstimateId,
                FieldName = request.Name,
                FieldKey = fieldId.ToString(),
                FieldType = (CostEstimateFieldType)request.FieldType,
                IsBasicField = false,
                IsAdditionalField = true,
                Order = order,
                CreatedAt = DateTime.UtcNow
            };

            await fieldSchemaRepository.Insert(field);
            await fieldSchemaRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Dodano nowe pole dodatkowe {FieldId} ({Name}) do kosztorysu {CostEstimateId}",
                field.Id,
                field.FieldName,
                request.CostEstimateId);

            return field.Id;
        }
    }
}
