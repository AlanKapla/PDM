using Business.Interfaces.Services;
using Business.Interfaces.WebModels.AI;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services.AI
{
    public sealed class AICostDocumentEnrichmentService : IAICostDocumentEnrichmentService
    {
        private readonly IContractorService contractorService;
        private readonly IReadRepository<ProjectCostCategory> categoryRepo;
        private readonly ILogger<AICostDocumentEnrichmentService> logger;

        public AICostDocumentEnrichmentService(
            IContractorService contractorService,
            IReadRepository<ProjectCostCategory> categoryRepo,
            ILogger<AICostDocumentEnrichmentService> logger)
        {
            this.contractorService = contractorService;
            this.categoryRepo = categoryRepo;
            this.logger = logger;
        }

        public async Task<ParsedCostDto> EnrichWithContractorAsync(
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
                Contractor? found = await contractorService.SearchByProfileAsync(
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
                logger.LogWarning(ex, "Failed to search contractor for name={Name}, nip={Nip}",
                    dto.ContractorName, dto.ContractorNip);
            }

            return dto;
        }

        public async Task<ParsedCostDto> EnrichWithCategoryAsync(
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
                IEnumerable<ProjectCostCategory> categories = await categoryRepo.GetBySearch(
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
                logger.LogWarning(ex, "Failed to search category for name={Name}", dto.CategoryName);
            }

            return dto;
        }
    }
}
