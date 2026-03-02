using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using MediatR;

namespace CQRS.CostEstimateTemplates.GetDefaultCostEstimateTemplateDetails
{
    /// <summary>
    /// Handler for retrieving full structure of a default (system) template by slug
    /// </summary>
    public class GetDefaultCostEstimateTemplateDetailsQueryHandler
        : IRequestHandler<GetDefaultCostEstimateTemplateDetailsQuery, CostEstimateTemplateStructureWeb>
    {
        private readonly ICostEstimateTemplateService costEstimateTemplateService;

        public GetDefaultCostEstimateTemplateDetailsQueryHandler(
            ICostEstimateTemplateService costEstimateTemplateService)
        {
            this.costEstimateTemplateService = costEstimateTemplateService;
        }

        public Task<CostEstimateTemplateStructureWeb> Handle(
            GetDefaultCostEstimateTemplateDetailsQuery request,
            CancellationToken cancellationToken)
        {
            var structure = costEstimateTemplateService.GetDefaultTemplateDetails(request.Slug);

            if (structure == null)
            {
                throw new NotFoundApiException("Default template", request.Slug);
            }

            return Task.FromResult(structure);
        }
    }
}
