using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using MediatR;

namespace CQRS.CostEstimates.GenerateCostEstimateAIPreview
{
    public sealed class GenerateCostEstimateAIPreviewCommandHandler
        : IRequestHandler<GenerateCostEstimateAIPreviewCommand, AICostEstimatePreviewWeb>
    {
        private readonly ICostEstimateAIGeneratorService aiGeneratorService;
        private readonly Business.Interfaces.Model.ICurrentUser currentUser;

        public GenerateCostEstimateAIPreviewCommandHandler(
            ICostEstimateAIGeneratorService aiGeneratorService,
            Business.Interfaces.Model.ICurrentUser currentUser)
        {
            this.aiGeneratorService = aiGeneratorService;
            this.currentUser = currentUser;
        }

        public async Task<AICostEstimatePreviewWeb> Handle(
            GenerateCostEstimateAIPreviewCommand request,
            CancellationToken cancellationToken)
        {
            // Template removed - AI generator uses hardcoded 9 basic fields
            return await aiGeneratorService.GeneratePreviewAsync(
                request.Request,
                cancellationToken);
        }
    }
}
