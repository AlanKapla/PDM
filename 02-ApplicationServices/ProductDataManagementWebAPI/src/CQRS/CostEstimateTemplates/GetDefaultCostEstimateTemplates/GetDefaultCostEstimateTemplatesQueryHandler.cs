using Business.Interfaces.Services;
using Business.Interfaces.WebModels.CostEstimateTemplates;
using MediatR;

namespace CQRS.CostEstimateTemplates.GetDefaultCostEstimateTemplates
{
    /// <summary>
    /// Handler for retrieving all available default (system) cost estimate templates
    /// </summary>
    public class GetDefaultCostEstimateTemplatesQueryHandler
        : IRequestHandler<GetDefaultCostEstimateTemplatesQuery, List<DefaultCostEstimateTemplateListItemWeb>>
    {
        private readonly ICostEstimateTemplateService costEstimateTemplateService;

        public GetDefaultCostEstimateTemplatesQueryHandler(
            ICostEstimateTemplateService costEstimateTemplateService)
        {
            this.costEstimateTemplateService = costEstimateTemplateService;
        }

        public Task<List<DefaultCostEstimateTemplateListItemWeb>> Handle(
            GetDefaultCostEstimateTemplatesQuery request,
            CancellationToken cancellationToken)
        {
            var templates = costEstimateTemplateService.GetDefaultTemplates();
            return Task.FromResult(templates);
        }
    }
}
