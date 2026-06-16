using Business.Implementation.Helpers;
using Business.Interfaces.Model;
using Entities.Models.CostEstimates;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.CreateCostEstimate
{
    /// <summary>
    /// Handler dla tworzenia kosztorysu.
    /// </summary>
    public sealed class CreateCostEstimateCommandHandler : IRequestHandler<CreateCostEstimateCommand, Guid>
    {
        private readonly IRepository<CostEstimate> costEstimateRepository;
        private readonly IRepository<CostEstimateFieldSchema> fieldSchemaRepository;
        private readonly ICurrentUser currentUser;

        public CreateCostEstimateCommandHandler(
            IRepository<CostEstimate> costEstimateRepository,
            IRepository<CostEstimateFieldSchema> fieldSchemaRepository,
            ICurrentUser currentUser)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.fieldSchemaRepository = fieldSchemaRepository;
            this.currentUser = currentUser;
        }

        public async Task<Guid> Handle(CreateCostEstimateCommand request, CancellationToken cancellationToken)
        {
            DateTime now = DateTime.UtcNow;
            Guid costEstimateId = Guid.NewGuid();

            CostEstimate costEstimate = new()
            {
                Id = costEstimateId,
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                OwnerId = currentUser.Id,
                Name = request.Name,
                Description = request.Description,
                Status = CostEstimateStatus.Draft,
                TotalNet = null,
                TotalGross = null,
                TotalVat = null,
                CreatedAt = now,
                IsDeleted = false
            };

            List<CostEstimateFieldSchema> defaultSchema =
                DefaultCostEstimateFieldSchemaFactory.CreateDefaultSchema(costEstimateId, now);

            await costEstimateRepository.Insert(costEstimate);
            await fieldSchemaRepository.InsertRange(defaultSchema);
            await costEstimateRepository.SaveChangesAsync(cancellationToken);

            return costEstimate.Id;
        }
    }
}
