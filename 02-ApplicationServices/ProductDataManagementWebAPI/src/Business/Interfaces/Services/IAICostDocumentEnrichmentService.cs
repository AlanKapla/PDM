using Business.Interfaces.WebModels.AI;

namespace Business.Interfaces.Services
{
    public interface IAICostDocumentEnrichmentService
    {
        Task<ParsedCostDto> EnrichWithContractorAsync(
            ParsedCostDto dto,
            Guid tenantId,
            CancellationToken cancellationToken);

        Task<ParsedCostDto> EnrichWithCategoryAsync(
            ParsedCostDto dto,
            Guid projectId,
            CancellationToken cancellationToken);
    }
}
