using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace CQRS.AI.ParseCostDocument
{
    public sealed class ParseCostDocumentQueryHandler
        : IRequestHandler<ParseCostDocumentQuery, ParsedCostDto>
    {
        private readonly IDocumentParserService parserService;
        private readonly IAICostDocumentEnrichmentService enrichmentService;

        public ParseCostDocumentQueryHandler(
            IDocumentParserService parserService,
            IAICostDocumentEnrichmentService enrichmentService)
        {
            this.parserService = parserService;
            this.enrichmentService = enrichmentService;
        }

        public async Task<ParsedCostDto> Handle(
            ParseCostDocumentQuery request,
            CancellationToken cancellationToken)
        {
            using MemoryStream ms = new();
            await request.File.CopyToAsync(ms, cancellationToken);
            byte[] fileBytes = ms.ToArray();

            string mediaType = request.File.ContentType.ToLowerInvariant();

            ParsedCostDto result = await parserService.ParseAsync(
                fileBytes, mediaType, cancellationToken);

            result = await enrichmentService.EnrichWithContractorAsync(
                result, request.TenantId, cancellationToken);
            result = await enrichmentService.EnrichWithCategoryAsync(
                result, request.ProjectId, cancellationToken);

            return result;
        }
    }
}
