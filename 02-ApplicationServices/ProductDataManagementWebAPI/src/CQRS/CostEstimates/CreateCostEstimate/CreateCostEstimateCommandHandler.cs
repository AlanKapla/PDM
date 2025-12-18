using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Models;
using Entities.Models.CostEstimateData;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.CreateCostEstimate
{
    /// <summary>
    /// Handler dla tworzenia pustego kosztorysu
    /// </summary>
    public class CreateCostEstimateCommandHandler : IRequestHandler<CreateCostEstimateCommand, Guid>
    {
        private readonly IRepository<CostEstimate> costEstimateRepository;
        private readonly IRepository<CostEstimateTemplate> templateRepository;
        private readonly ICurrentUser currentUser;

        public CreateCostEstimateCommandHandler(
            IRepository<CostEstimate> costEstimateRepository,
            IRepository<CostEstimateTemplate> templateRepository,
            ICurrentUser currentUser)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.templateRepository = templateRepository;
            this.currentUser = currentUser;
        }

        public async Task<Guid> Handle(CreateCostEstimateCommand request, CancellationToken cancellationToken)
        {
            // Validate tenant isolation
            if (request.TenantId != currentUser.ActiveTenantId)
            {
                throw new ForbiddenApiException("Cannot create cost estimate in a different tenant");
            }

            // Verify template exists
            var template = await templateRepository.GetFirstBySearch(
                t => t.Id == request.TemplateId && !t.IsDeleted);

            if (template == null)
            {
                throw new NotFoundApiException(nameof(CostEstimateTemplate), request.TemplateId.ToString());
            }

            // Create empty cost estimate with minimal metadata
            var emptyData = new CostEstimateDataModel
            {
                Groups = new List<CostEstimateGroup>(),
                Metadata = new CostEstimateMetadata
                {
                    LastModified = DateTime.UtcNow,
                    LastModifiedBy = currentUser.Id,
                    SchemaVersion = 1
                }
            };

            // Create entity
            var costEstimate = new CostEstimate
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                TemplateId = request.TemplateId,
                OwnerId = currentUser.Id,
                Name = request.Name,
                Description = request.Description,
                Status = CostEstimateStatus.Draft,
                Data = emptyData,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            // Insert and save
            await costEstimateRepository.Insert(costEstimate);
            await costEstimateRepository.SaveChangesAsync(cancellationToken);

            return costEstimate.Id;
        }
    }
}
