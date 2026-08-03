using Entities.Models.Tenants;

namespace Business.Interfaces.Services
{
    public interface IContractorService
    {
        Task<Dictionary<Guid, string>> GetNamesByIdsAsync(
            IReadOnlyCollection<Guid> ids, Guid tenantId, CancellationToken cancellationToken);

        Task<bool> AreAllInTenantAsync(
            Guid tenantId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

        /// <summary>
        /// Wyszukuje kontrahenta po profilu z dokumentu.
        /// Kolejność dopasowania: NIP (dokładne) → Nazwa (zawiera, case-insensitive).
        /// Zwraca null jeśli brak dopasowania.
        /// </summary>
        Task<Contractor?> SearchByProfileAsync(
            string? name,
            string? taxId,
            Guid tenantId,
            CancellationToken cancellationToken);
    }
}
