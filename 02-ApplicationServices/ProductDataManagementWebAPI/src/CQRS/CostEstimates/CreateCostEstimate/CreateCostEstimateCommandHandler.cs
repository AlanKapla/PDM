using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.CreateCostEstimate
{
    /// <summary>
    /// Handler dla tworzenia kosztorysu
    /// Obsługuje tworzenie pustego kosztorysu lub z pełną hierarchią grup/pozycji
    /// Waliduje strukturę grup i wartości pól przed utworzeniem
    /// Automatycznie przelicza sumy po utworzeniu
    /// </summary>
    public sealed class CreateCostEstimateCommandHandler : IRequestHandler<CreateCostEstimateCommand, Guid>
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
            // Verify template exists (field definitions are not needed at create-time)
            CostEstimateTemplate template = await templateRepository.GetFirstBySearch(
                t => t.Id == request.TemplateId && !t.IsDeleted && t.OwnerId == currentUser.Id)
                ?? throw new NotFoundApiException(nameof(CostEstimateTemplate), request.TemplateId.ToString());

            DateTime now = DateTime.UtcNow;

            // Create cost estimate
            CostEstimate costEstimate = new CostEstimate
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                TemplateId = request.TemplateId,
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

            await costEstimateRepository.Insert(costEstimate);
            await costEstimateRepository.SaveChangesAsync(cancellationToken);

            return costEstimate.Id;
        }
    }
}
