using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using Entities.Models.CostEstimateTemplates;
using MediatR;

namespace CQRS.CostEstimates.GenerateCostEstimateAIPreview
{
    public sealed class GenerateCostEstimateAIPreviewCommandHandler
        : IRequestHandler<GenerateCostEstimateAIPreviewCommand, AICostEstimatePreviewWeb>
    {
        private readonly ICostEstimateTemplateService templateService;
        private readonly ICostEstimateAIGeneratorService aiGeneratorService;
        private readonly Business.Interfaces.Model.ICurrentUser currentUser;

        public GenerateCostEstimateAIPreviewCommandHandler(
            ICostEstimateTemplateService templateService,
            ICostEstimateAIGeneratorService aiGeneratorService,
            Business.Interfaces.Model.ICurrentUser currentUser)
        {
            this.templateService = templateService;
            this.aiGeneratorService = aiGeneratorService;
            this.currentUser = currentUser;
        }

        public async Task<AICostEstimatePreviewWeb> Handle(
            GenerateCostEstimateAIPreviewCommand request,
            CancellationToken cancellationToken)
        {
            CostEstimateTemplate template = await templateService.GetTemplateForAIGenerationAsync(
                request.Request.TemplateId,
                currentUser.Id,
                cancellationToken);

            return await aiGeneratorService.GeneratePreviewAsync(
                request.Request,
                template,
                cancellationToken);
        }
    }
}
