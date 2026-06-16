using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.CostEstimates;
using Entities.Models.CostEstimates;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.GetAdditionalFields
{
    public sealed class GetAdditionalFieldsQueryHandler : IRequestHandler<GetAdditionalFieldsQuery, List<CostEstimateAdditionalFieldWeb>>
    {
        private readonly IReadRepository<CostEstimate> costEstimateRepository;
        private readonly IReadRepository<CostEstimateFieldSchema> fieldSchemaRepository;

        public GetAdditionalFieldsQueryHandler(
            IReadRepository<CostEstimate> costEstimateRepository,
            IReadRepository<CostEstimateFieldSchema> fieldSchemaRepository)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.fieldSchemaRepository = fieldSchemaRepository;
        }

        public async Task<List<CostEstimateAdditionalFieldWeb>> Handle(
            GetAdditionalFieldsQuery request,
            CancellationToken cancellationToken)
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

            IEnumerable<CostEstimateFieldSchema> fields = await fieldSchemaRepository.GetBySearch(
                f => f.CostEstimateId == request.CostEstimateId && f.IsAdditionalField);

            return fields
                .OrderBy(f => f.Order)
                .Select(MapToAdditionalFieldWeb)
                .ToList();
        }

        internal static CostEstimateAdditionalFieldWeb MapToAdditionalFieldWeb(CostEstimateFieldSchema field)
        {
            return new CostEstimateAdditionalFieldWeb(
                Id: field.Id,
                CostEstimateId: field.CostEstimateId,
                Name: field.FieldName,
                FieldType: (int)field.FieldType,
                Order: field.Order,
                CreatedAt: field.CreatedAt,
                UpdatedAt: field.UpdatedAt
            );
        }

        internal static CostEstimateFieldSchemaWeb MapToFieldSchemaWeb(CostEstimateFieldSchema field)
        {
            return new CostEstimateFieldSchemaWeb(
                Id: field.Id,
                CostEstimateId: field.CostEstimateId,
                FieldName: field.FieldName,
                FieldKey: field.FieldKey,
                FieldType: (int)field.FieldType,
                IsBasicField: field.IsBasicField,
                IsAdditionalField: field.IsAdditionalField,
                Order: field.Order,
                CreatedAt: field.CreatedAt,
                UpdatedAt: field.UpdatedAt
            );
        }
    }
}
