using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimates.DeleteCostEstimateTemplate
{
    /// <summary>
    /// Handler dla usuwania szablonu kosztorysu (soft delete)
    /// </summary>
    public class DeleteCostEstimateTemplateCommandHandler : IRequestHandler<DeleteCostEstimateTemplateCommand, Unit>
    {
        private readonly IRepository<CostEstimateTemplate> templateRepository;
        private readonly ICurrentUser currentUser;

        public DeleteCostEstimateTemplateCommandHandler(
            IRepository<CostEstimateTemplate> templateRepository,
            ICurrentUser currentUser)
        {
            this.templateRepository = templateRepository;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(DeleteCostEstimateTemplateCommand request, CancellationToken cancellationToken)
        {
            // Get existing template - filter by OwnerId in database
            var template = await templateRepository.GetFirstBySearch(
                t => t.Id == request.TemplateId && t.OwnerId == currentUser.Id && !t.IsDeleted)
             ?? throw new NotFoundApiException(nameof(CostEstimateTemplate), request.TemplateId.ToString());

            // Soft delete
            template.IsDeleted = true;
            template.DeletedAt = DateTime.UtcNow;

            // Save changes
            await templateRepository.Update(template);

            return Unit.Value;
        }
    }
}
