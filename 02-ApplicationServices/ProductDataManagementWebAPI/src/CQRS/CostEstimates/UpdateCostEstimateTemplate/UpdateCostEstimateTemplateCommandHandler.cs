using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.UpdateCostEstimateTemplate
{
    /// <summary>
    /// Handler dla aktualizacji szablonu kosztorysu
    /// </summary>
    public class UpdateCostEstimateTemplateCommandHandler : IRequestHandler<UpdateCostEstimateTemplateCommand, Unit>
    {
        private readonly IRepository<CostEstimateTemplate> templateRepository;
        private readonly ICurrentUser currentUser;

        public UpdateCostEstimateTemplateCommandHandler(
            IRepository<CostEstimateTemplate> templateRepository,
            ICurrentUser currentUser)
        {
            this.templateRepository = templateRepository;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(UpdateCostEstimateTemplateCommand request, CancellationToken cancellationToken)
        {
            // Get existing template - filter by OwnerId in database
            var template = await templateRepository.GetFirstBySearch(
                t => t.Id == request.TemplateId && t.OwnerId == currentUser.Id && !t.IsDeleted);

            if (template == null)
            {
                throw new NotFoundApiException(nameof(CostEstimateTemplate), request.TemplateId.ToString());
            }

            // Update properties - use TemplateStructure directly from command
            template.Name = request.Name;
            template.Description = request.Description;
            template.TemplateStructure = request.TemplateStructure;
            template.UpdatedAt = DateTime.UtcNow;

            // Save changes
            await templateRepository.Update(template);
            await templateRepository.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
