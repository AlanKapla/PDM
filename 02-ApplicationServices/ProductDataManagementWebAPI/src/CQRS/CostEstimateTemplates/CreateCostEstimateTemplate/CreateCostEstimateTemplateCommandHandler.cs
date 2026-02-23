using Business.Interfaces.Model;
using Business.Interfaces.Services;
using MediatR;

namespace CQRS.CostEstimateTemplates.CreateCostEstimateTemplate
{
    /// <summary>
    /// Handler for creating a cost estimate template
    /// </summary>
    public class CreateCostEstimateTemplateCommandHandler : IRequestHandler<CreateCostEstimateTemplateCommand, Guid>
    {
        private readonly ICostEstimateTemplateService costEstimateTemplateService;
        private readonly ICurrentUser currentUser;

        public CreateCostEstimateTemplateCommandHandler(
            ICostEstimateTemplateService costEstimateTemplateService,
            ICurrentUser currentUser)
        {
            this.costEstimateTemplateService = costEstimateTemplateService;
            this.currentUser = currentUser;
        }

        public async Task<Guid> Handle(CreateCostEstimateTemplateCommand request, CancellationToken cancellationToken)
        {
            return await costEstimateTemplateService.CreateTemplateAsync(
                currentUser.Id,
                request.Name,
                request.Description,
                cancellationToken);
        }
    }
}
