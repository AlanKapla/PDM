using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using CQRS.CostEstimates.GetCostEstimateTemplateDetails;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositiories.Repository.Interfaces;

namespace CQRS.CostEstimates.GetCostEstimateTemplateDetails
{
    /// <summary>
    /// Handler dla pobrania szczegółów szablonu kosztorysu
    /// </summary>
    public class GetCostEstimateTemplateDetailsQueryHandler : IRequestHandler<GetCostEstimateTemplateDetailsQuery, CostEstimateTemplateDetails>
    {
        private readonly IReadRepository<CostEstimateTemplate> templateRepository;
        private readonly ICurrentUser currentUser;

        public GetCostEstimateTemplateDetailsQueryHandler(
            IReadRepository<CostEstimateTemplate> templateRepository,
            ICurrentUser currentUser)
        {
            this.templateRepository = templateRepository;
            this.currentUser = currentUser;
        }

        public async Task<CostEstimateTemplateDetails> Handle(GetCostEstimateTemplateDetailsQuery request, CancellationToken cancellationToken)
        {
            // Get template with Owner - filter by OwnerId in database
            var template = await templateRepository.GetFirstBySearch(
                t => t.Id == request.TemplateId && t.OwnerId == currentUser.Id && !t.IsDeleted,
                cancellationToken,
                q => q.Include(t => t.Owner));

            if (template == null)
            {
                throw new NotFoundApiException(nameof(CostEstimateTemplate), request.TemplateId.ToString());
            }

            // Return entity type directly - no mapping needed!
            return new CostEstimateTemplateDetails(
                Id: template.Id,
                Name: template.Name,
                Description: template.Description,
                CreatedAt: template.CreatedAt,
                UpdatedAt: template.UpdatedAt,
                OwnerId: template.OwnerId,
                OwnerName: $"{template.Owner.FirstName} {template.Owner.LastName}",
                TemplateStructure: template.TemplateStructure
            );
        }
    }
}
