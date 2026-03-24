using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.CostEstimateTemplates;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.CostEstimateTemplates.DuplicateCostEstimateTemplate
{
    public class DuplicateCostEstimateTemplateCommandHandler
        : IRequestHandler<DuplicateCostEstimateTemplateCommand, Guid>
    {
        private readonly IRepository<CostEstimateTemplate> templateRepository;
        private readonly ICostEstimateTemplateService costEstimateTemplateService;
        private readonly ICurrentUser currentUser;

        public DuplicateCostEstimateTemplateCommandHandler(
            IRepository<CostEstimateTemplate> templateRepository,
            ICostEstimateTemplateService costEstimateTemplateService,
            ICurrentUser currentUser)
        {
            this.templateRepository = templateRepository;
            this.costEstimateTemplateService = costEstimateTemplateService;
            this.currentUser = currentUser;
        }

        public async Task<Guid> Handle(DuplicateCostEstimateTemplateCommand request, CancellationToken cancellationToken)
        {
            var sourceTemplate = await templateRepository.GetFirstBySearch(
                t => t.Id == request.SourceTemplateId && t.OwnerId == currentUser.Id && !t.IsDeleted)
                ?? throw new NotFoundApiException(nameof(CostEstimateTemplate), request.SourceTemplateId.ToString());

            return await costEstimateTemplateService.DuplicateTemplateAsync(
                sourceTemplate,
                currentUser.Id,
                request.Name,
                request.Description,
                cancellationToken);
        }
    }
}
