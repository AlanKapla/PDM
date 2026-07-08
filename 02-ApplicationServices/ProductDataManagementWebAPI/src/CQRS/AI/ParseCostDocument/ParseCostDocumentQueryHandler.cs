using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.AI.ParseCostDocument
{
    public sealed class ParseCostDocumentQueryHandler
        : IRequestHandler<ParseCostDocumentQuery, ParsedCostDto>
    {
        private readonly IDocumentParserService _parserService;
        private readonly IContractorService _contractorService;
        private readonly IReadRepository<ProjectCostCategory> _categoryRepo;
        private readonly ILogger<ParseCostDocumentQueryHandler> _logger;

        public ParseCostDocumentQueryHandler(
            IDocumentParserService parserService,
            IContractorService contractorService,
            IReadRepository<ProjectCostCategory> categoryRepo,
            ILogger<ParseCostDocumentQueryHandler> logger)
        {
            _parserService = parserService;
            _contractorService = contractorService;
            _categoryRepo = categoryRepo;
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
            result = await EnrichWithCategoryAsync(result, request.ProjectId, cancellationToken);

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

        private async Task<ParsedCostDto> EnrichWithCategoryAsync(
            ParsedCostDto dto,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dto.CategoryName))
            {
                return dto;
            }

            try
            {
                IEnumerable<ProjectCostCategory> categories = await _categoryRepo.GetBySearch(
                    c => c.ProjectId == projectId);

                string normalizedInput = dto.CategoryName.Trim();

                ProjectCostCategory? exactMatch = categories.FirstOrDefault(c =>
                    string.Equals(c.Name, normalizedInput, StringComparison.OrdinalIgnoreCase)
                    || (c.Code is not null && string.Equals(c.Code, normalizedInput, StringComparison.OrdinalIgnoreCase)));

                if (exactMatch is not null)
                {
                    return dto with
                    {
                        CategoryId = exactMatch.Id,
                        CategoryFound = true,
                        SuggestedCategory = null
                    };
                }

                ProjectCostCategory? containsMatch = categories.FirstOrDefault(c =>
                    c.Name.Contains(normalizedInput, StringComparison.OrdinalIgnoreCase)
                    || normalizedInput.Contains(c.Name, StringComparison.OrdinalIgnoreCase));

                if (containsMatch is not null)
                {
                    return dto with
                    {
                        CategoryId = containsMatch.Id,
                        CategoryFound = true,
                        SuggestedCategory = null
                    };
                }

                return dto with
                {
                    CategoryFound = false,
                    SuggestedCategory = new SuggestedCostCategoryDto
                    {
                        Name = normalizedInput
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to search category for name={Name}", dto.CategoryName);
            }

            return dto;
        }
    }
}
