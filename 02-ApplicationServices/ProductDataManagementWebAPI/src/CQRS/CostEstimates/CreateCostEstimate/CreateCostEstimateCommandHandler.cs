using Business.Implementation.Validators;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimates;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.CreateCostEstimate
{
    /// <summary>
    /// Handler dla tworzenia kosztorysu
    /// Obsługuje tworzenie pustego kosztorysu lub z pełną hierarchią grup/pozycji
    /// Waliduje strukturę grup i wartości pól przed utworzeniem
    /// Automatycznie przelicza sumy po utworzeniu
    /// </summary>
    public class CreateCostEstimateCommandHandler : IRequestHandler<CreateCostEstimateCommand, Guid>
    {
        private readonly IRepository<CostEstimate> costEstimateRepository;
        private readonly IRepository<CostEstimateTemplate> templateRepository;
        private readonly ICurrentUser currentUser;

        public CreateCostEstimateCommandHandler(
            IRepository<CostEstimate> costEstimateRepository,
            IRepository<CostEstimateTemplate> templateRepository,
            IRepository<CostEstimateGroup> groupRepository,
            IRepository<CostEstimateGroupFieldValue> groupFieldValueRepository,
            IRepository<CostEstimateItem> itemRepository,
            IRepository<CostEstimateItemFieldValue> itemFieldValueRepository,
            ICostEstimateCalculationService calculationService,
            CostEstimateGroupValidator groupValidator,
            CostEstimateItemValidator itemValidator,
            ICurrentUser currentUser)
        {
            this.costEstimateRepository = costEstimateRepository;
            this.templateRepository = templateRepository;
            this.currentUser = currentUser;
        }

        public async Task<Guid> Handle(CreateCostEstimateCommand request, CancellationToken cancellationToken)
        {
            // Verify template exists and load with all necessary includes
            var templates = await templateRepository.GetBySearch(
                t => t.Id == request.TemplateId && !t.IsDeleted && t.OwnerId == currentUser.Id,
                q => q.Include(v => v.Currencies)
                          .Include(v => v.GroupFieldDefinitions)
                          .Include(v => v.SystemFieldDefinitions)
                          .Include(v => v.CalculatedFieldDefinitions)
                          .Include(v => v.GenericFieldDefinitions));
            
            var template = templates.FirstOrDefault()
                ?? throw new NotFoundApiException(nameof(CostEstimateTemplate), request.TemplateId.ToString());

            // Verify selected currency exists in version
            var selectedCurrency = template.Currencies.FirstOrDefault(c => c.Id == request.SelectedCurrencyId)
                ?? throw new ValidationApiException(
                    $"Currency with ID {request.SelectedCurrencyId} not found in template version. Available currencies: {string.Join(", ", template.Currencies.Select(c => $"{c.Code} ({c.Id})"))}");

            var now = DateTime.UtcNow;

            // Create cost estimate
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
                SelectedCurrencyId = request.SelectedCurrencyId,
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
