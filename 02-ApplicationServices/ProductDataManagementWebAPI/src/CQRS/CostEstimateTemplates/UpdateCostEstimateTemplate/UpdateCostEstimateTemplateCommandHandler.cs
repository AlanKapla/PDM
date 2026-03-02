using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimateTemplates;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimateTemplates.UpdateCostEstimateTemplate
{
    public class UpdateCostEstimateTemplateCommandHandler : IRequestHandler<UpdateCostEstimateTemplateCommand, Unit>
    {
        private readonly IRepository<CostEstimateTemplate> templateRepository;
        private readonly ICostEstimateTemplateService costEstimateTemplateService;
        private readonly ICostEstimateService costEstimateService;
        private readonly ICurrentUser currentUser;

        public UpdateCostEstimateTemplateCommandHandler(
            IRepository<CostEstimateTemplate> templateRepository,
            ICostEstimateTemplateService costEstimateTemplateService,
            ICostEstimateService costEstimateService,
            ICurrentUser currentUser)
        {
            this.templateRepository = templateRepository;
            this.costEstimateTemplateService = costEstimateTemplateService;
            this.costEstimateService = costEstimateService;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(UpdateCostEstimateTemplateCommand request, CancellationToken cancellationToken)
        {
            var template = await templateRepository.GetFirstBySearch(
                t => t.Id == request.TemplateId && t.OwnerId == currentUser.Id && !t.IsDeleted)
                ?? throw new NotFoundApiException(nameof(CostEstimateTemplate), request.TemplateId.ToString());

            await costEstimateTemplateService.UpdateTemplateAsync(
                template,
                request.Name,
                request.Description,
                request.Category,
                request.CanAddGroups,
                request.CanBranchGroups,
                request.MaxGroupLevel,
                request.AutoNumberGroups,
                request.GroupNumberFormat,
                request.UpdateStructure,
                request.Currencies,
                request.Units,
                request.GroupHeaderFields,
                request.SystemFields,
                request.CalculatedFields,
                request.GenericFields,
                request.UiConfiguration,
                cancellationToken);

            if (request.UpdateStructure)
            {
                await costEstimateService.AddSelectedFieldToExistingItemsAsync(request.TemplateId, cancellationToken);
            }

            return Unit.Value;
        }
    }
}
