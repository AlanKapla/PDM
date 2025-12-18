using Business.Interfaces.Model;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.CreateCostEstimateTemplate
{
    /// <summary>
    /// Handler dla tworzenia szablonu kosztorysu
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
            // Create entity - use TemplateStructure directly from command
            var template = new CostEstimateTemplate
            {
                Id = Guid.NewGuid(),
                OwnerId = currentUser.Id,
                Name = request.Name,
                Description = request.Description,
                TemplateStructure = request.TemplateStructure,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            // Insert and save
            await templateRepository.Insert(template);
            await templateRepository.SaveChangesAsync(cancellationToken);

            return template.Id;
        }
    }
}
