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
            // Get template with Owner
            // SuperAdmin can access templates without ownership check for monitoring purposes
            // Regular users are filtered by OwnerId in the authorization layer
            var template = await templateRepository.GetFirstBySearch(
                t => t.Id == request.TemplateId && !t.IsDeleted,
                cancellationToken,
                q => q.Include(t => t.Owner));

            if (template == null)
            {
                throw new NotFoundApiException(nameof(CostEstimateTemplate), request.TemplateId.ToString());
            }

            // Additional ownership check for non-SuperAdmin users
            // SuperAdmin can view any template for monitoring/auditing
            if (!currentUser.IsSuperAdmin && template.OwnerId != currentUser.Id)
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
