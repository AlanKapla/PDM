namespace Business.Interfaces.Services
{
    public interface IContractorService
    {
        Task<Dictionary<Guid, string>> GetNamesByIdsAsync(
            IReadOnlyCollection<Guid> ids, Guid tenantId, CancellationToken cancellationToken);
    }
}
