using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using Entities.Models.Tenants;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CQRS.AI.ParseCostDocument
{
    public sealed class ParseCostDocumentQueryHandler
        : IRequestHandler<ParseCostDocumentQuery, ParsedCostDto>
    {
        private readonly IDocumentParserService _parserService;
        private readonly IContractorService _contractorService;
        private readonly ILogger<ParseCostDocumentQueryHandler> _logger;

        public ParseCostDocumentQueryHandler(
            IDocumentParserService parserService,
            IContractorService contractorService,
            ILogger<ParseCostDocumentQueryHandler> logger)
        {
            _parserService = parserService;
            _contractorService = contractorService;
            _logger = logger;
        }

        public async Task<ParsedCostDto> Handle(
            ParseCostDocumentQuery request,
            CancellationToken cancellationToken)
        {
            using MemoryStream ms = new();
            await request.File.CopyToAsync(ms, cancellationToken);
            byte[] fileBytes = ms.ToArray();

            string mediaType = request.File.ContentType.ToLowerInvariant();

            ParsedCostDto result = await _parserService.ParseAsync(
                fileBytes, mediaType, cancellationToken);

            result = await EnrichWithContractorAsync(result, request.TenantId, cancellationToken);

            return result;
        }

        private async Task<ParsedCostDto> EnrichWithContractorAsync(
            ParsedCostDto dto,
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dto.ContractorName) &&
                string.IsNullOrWhiteSpace(dto.ContractorNip))
            {
                return dto;
            }

            try
            {
                Contractor? found = await _contractorService.SearchByProfileAsync(
                    dto.ContractorName,
                    dto.ContractorNip,
                    tenantId,
                    cancellationToken);

                if (found is not null)
                {
                    return dto with
                    {
                        ContractorId = found.Id,
                        ContractorFound = true,
                        SuggestedContractor = null
                    };
                }

                if (!string.IsNullOrWhiteSpace(dto.ContractorName))
                {
                    return dto with
                    {
                        ContractorFound = false,
                        SuggestedContractor = new SuggestedContractorDto
                        {
                            Name = dto.ContractorName,
                            Nip = dto.ContractorNip,
                            Address = dto.ContractorAddress
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to search contractor for name={Name}, nip={Nip}",
                    dto.ContractorName, dto.ContractorNip);
            }

            return dto;
        }
    }
}
