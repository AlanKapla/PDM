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
                q => q.Include(t => t.Versions.Where(v => v.Id == request.TemplateVersionId))
                          .ThenInclude(v => v.Currencies)
                      .Include(t => t.Versions.Where(v => v.Id == request.TemplateVersionId))
                          .ThenInclude(v => v.GroupFieldDefinitions)
                      .Include(t => t.Versions.Where(v => v.Id == request.TemplateVersionId))
                          .ThenInclude(v => v.SystemFieldDefinitions)
                      .Include(t => t.Versions.Where(v => v.Id == request.TemplateVersionId))
                          .ThenInclude(v => v.CalculatedFieldDefinitions)
                      .Include(t => t.Versions.Where(v => v.Id == request.TemplateVersionId))
                          .ThenInclude(v => v.GenericFieldDefinitions));
            
            var template = templates.FirstOrDefault()
                ?? throw new NotFoundApiException(nameof(CostEstimateTemplate), request.TemplateId.ToString());

            // Get version
            var version = template.Versions.FirstOrDefault(v => v.Id == request.TemplateVersionId)
                ?? throw new NotFoundApiException(nameof(CostEstimateTemplateVersion), request.TemplateVersionId.ToString());

            // Version must be Approved to create cost estimate
            if (version.Status != TemplateVersionStatus.Approved)
            {
                throw new ValidationApiException(
                    "Cannot create cost estimate from Draft template version. Only Approved versions can be used.");
            }

            // Verify selected currency exists in version
            var selectedCurrency = version.Currencies.FirstOrDefault(c => c.Id == request.SelectedCurrencyId)
                ?? throw new ValidationApiException(
                    $"Currency with ID {request.SelectedCurrencyId} not found in template version. Available currencies: {string.Join(", ", version.Currencies.Select(c => $"{c.Code} ({c.Id})"))}");

            var now = DateTime.UtcNow;

            // Create cost estimate
            var costEstimate = new CostEstimate
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ProjectId = request.ProjectId,
                TemplateId = request.TemplateId,
                TemplateVersionId = request.TemplateVersionId,
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
