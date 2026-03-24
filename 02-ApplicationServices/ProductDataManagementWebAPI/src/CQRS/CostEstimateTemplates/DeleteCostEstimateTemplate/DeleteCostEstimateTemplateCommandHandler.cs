using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimateTemplates;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimateTemplates.DeleteCostEstimateTemplate
{
    public class DeleteCostEstimateTemplateCommandHandler
        : IRequestHandler<DeleteCostEstimateTemplateCommand, Unit>
    {
        private readonly IRepository<CostEstimateTemplate> templateRepository;
        private readonly ICostEstimateTemplateService costEstimateTemplateService;
        private readonly ICurrentUser currentUser;

        public DeleteCostEstimateTemplateCommandHandler(
            IRepository<CostEstimateTemplate> templateRepository,
            ICostEstimateTemplateService costEstimateTemplateService,
            ICurrentUser currentUser)
        {
            this.templateRepository = templateRepository;
            this.costEstimateTemplateService = costEstimateTemplateService;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(DeleteCostEstimateTemplateCommand request, CancellationToken cancellationToken)
        {
            var template = await templateRepository.GetFirstBySearch(
                t => t.Id == request.TemplateId && t.OwnerId == currentUser.Id && !t.IsDeleted)
                ?? throw new NotFoundApiException(nameof(CostEstimateTemplate), request.TemplateId.ToString());

            await costEstimateTemplateService.DeleteTemplateAsync(template, cancellationToken);

            return Unit.Value;
        }
    }
}
