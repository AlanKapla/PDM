using Business.Interfaces.Model;
using Business.Interfaces.Services;
using MediatR;

namespace CQRS.CostEstimateTemplates.CreateCostEstimateTemplateFromDefault
{
    /// <summary>
    /// Creates a new user template by copying the full structure from a default (system) template
    /// </summary>
    public class CreateCostEstimateTemplateFromDefaultCommandHandler
        : IRequestHandler<CreateCostEstimateTemplateFromDefaultCommand, Guid>
    {
        private readonly ICostEstimateTemplateService costEstimateTemplateService;
        private readonly ICurrentUser currentUser;

        public CreateCostEstimateTemplateFromDefaultCommandHandler(
            ICostEstimateTemplateService costEstimateTemplateService,
            ICurrentUser currentUser)
        {
            this.costEstimateTemplateService = costEstimateTemplateService;
            this.currentUser = currentUser;
        }

        public async Task<Guid> Handle(
            CreateCostEstimateTemplateFromDefaultCommand request,
            CancellationToken cancellationToken)
        {
            return await costEstimateTemplateService.CreateTemplateFromDefaultAsync(
                currentUser.Id,
                request.Slug,
                request.Name,
                request.Description,
                cancellationToken);
        }
    }
}
