namespace Business.Interfaces.Services
{
    public interface ICostEstimateRecalculationService
    {
        Task RecalculateAsync(
            Guid tenantId,
            Guid projectId,
            Guid costEstimateId,
            CancellationToken cancellationToken = default);
    }
}
