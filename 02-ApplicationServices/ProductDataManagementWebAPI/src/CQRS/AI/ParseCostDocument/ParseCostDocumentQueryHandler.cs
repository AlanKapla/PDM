using Business.Interfaces.Exceptions;
using Business.Interfaces.Helpers;
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
            byte[] fileBytes = await ReadFileBytesAsync(request.File, cancellationToken);
            ValidateFileContent(fileBytes, request.File);

            string mediaType = request.File.ContentType.ToLowerInvariant();
            ParsedCostDto result = await ParseDocumentAsync(fileBytes, mediaType, cancellationToken);

            result = await enrichmentService.EnrichWithContractorAsync(
                result, request.TenantId, cancellationToken);
            result = await enrichmentService.EnrichWithCategoryAsync(
                result, request.ProjectId, cancellationToken);

            return result;
        }

        private static async Task<byte[]> ReadFileBytesAsync(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            using MemoryStream ms = new();
            await file.CopyToAsync(ms, cancellationToken);
            return ms.ToArray();
        }

        private static void ValidateFileContent(byte[] fileBytes, IFormFile file)
        {
            FileContentValidator.FileValidationResult validation = FileContentValidator.ValidateBytes(
                fileBytes,
                file.FileName,
                file.ContentType);

            if (!validation.IsSuccess)
            {
                throw new ValidationApiException(validation.FailureReason!);
            }
        }

        private async Task<ParsedCostDto> ParseDocumentAsync(
            byte[] fileBytes,
            string mediaType,
            CancellationToken cancellationToken)
        {
            try
            {
                return await parserService.ParseAsync(fileBytes, mediaType, cancellationToken);
            }
            catch (PdfConversionException ex)
            {
                throw new ValidationApiException(ex.UserMessage);
            }
        }
    }
}
