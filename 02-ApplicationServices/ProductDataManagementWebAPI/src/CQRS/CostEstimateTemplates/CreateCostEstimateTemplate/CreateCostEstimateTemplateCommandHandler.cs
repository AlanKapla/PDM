using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using MediatR;
using Repositories.Repository.Interfaces;
using Entities.Models.CostEstimateTemplates;

namespace CQRS.CostEstimateTemplates.CreateCostEstimateTemplate
{
    /// <summary>
    /// Handler dla tworzenia szablonu kosztorysu
    /// Tworzy szablon z domyślną konfiguracją
    /// Cała struktura (pola, waluty, jednostki, konfiguracje) jest dodawana przez UpdateCostEstimateTemplate
    /// </summary>
    public class CreateCostEstimateTemplateCommandHandler : IRequestHandler<CreateCostEstimateTemplateCommand, Guid>
    {
        private readonly IRepository<CostEstimateTemplate> templateRepository;
        private readonly ICurrentUser currentUser;

        public CreateCostEstimateTemplateCommandHandler(
            IRepository<CostEstimateTemplate> templateRepository,
            ICurrentUser currentUser)
        {
            this.templateRepository = templateRepository;
            this.currentUser = currentUser;
        }

        public async Task<Guid> Handle(CreateCostEstimateTemplateCommand request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            var template = new CostEstimateTemplate
            {
                Id = Guid.NewGuid(),
                OwnerId = currentUser.Id,
                Name = request.Name,
                Description = request.Description,
                Category = null,
                CanAddGroups = true,
                CanBranchGroups = true,
                MaxGroupLevel = null,
                AutoNumberGroups = false,
                GroupNumberFormat = null,
                CreatedAt = now,
                IsDeleted = false
            };

            await templateRepository.Insert(template);
            await templateRepository.SaveChangesAsync(cancellationToken);

            return template.Id;
        }
    }
}
