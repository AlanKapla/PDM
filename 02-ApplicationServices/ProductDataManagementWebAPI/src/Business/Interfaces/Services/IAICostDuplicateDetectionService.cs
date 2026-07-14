using Business.Interfaces.WebModels.AI;

namespace Business.Interfaces.Services
{
    public interface IAICostDuplicateDetectionService
    {
        Task<bool> IsDuplicateAsync(
            Guid tenantId,
            Guid projectId,
            string fileHashSha256,
            ParsedCostDto parsedData,
            Guid? excludeItemId,
            CancellationToken cancellationToken);
    }
}
