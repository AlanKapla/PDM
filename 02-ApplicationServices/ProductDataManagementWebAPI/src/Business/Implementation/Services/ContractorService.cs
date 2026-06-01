using Business.Interfaces.Services;
using Entities.Models.Tenants;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public sealed class ContractorService : IContractorService
    {
        private readonly IReadRepository<Contractor> contractorRepo;

        public ContractorService(IReadRepository<Contractor> contractorRepo)
        {
            this.contractorRepo = contractorRepo;
        }

        public async Task<Dictionary<Guid, string>> GetNamesByIdsAsync(
            IReadOnlyCollection<Guid> ids, Guid tenantId, CancellationToken cancellationToken)
        {
            if (ids.Count == 0)
            {
                return new Dictionary<Guid, string>();
            }

            Dictionary<Guid, Contractor> contractorsDict = await contractorRepo.GetDictionaryBySearchAsync(
                c => ids.Contains(c.Id) && c.TenantId == tenantId && !c.IsDeleted,
                cancellationToken);

            return contractorsDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Name);
        }

        public async Task<Contractor?> SearchByProfileAsync(
            string? name,
            string? taxId,
            Guid tenantId,
            CancellationToken cancellationToken)
        {
            IEnumerable<Contractor> contractors = await contractorRepo.GetBySearch(
                c => c.TenantId == tenantId && !c.IsDeleted);

            if (!string.IsNullOrWhiteSpace(taxId))
            {
                string normalizedTaxId = NormalizeTaxId(taxId);
                Contractor? byTaxId = contractors.FirstOrDefault(c =>
                    !string.IsNullOrWhiteSpace(c.TaxId) &&
                    NormalizeTaxId(c.TaxId) == normalizedTaxId);

                if (byTaxId is not null)
                {
                    return byTaxId;
                }
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                Contractor? byName = contractors.FirstOrDefault(c =>
                    c.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

                if (byName is not null)
                {
                    return byName;
                }
            }

            return null;
        }

        private static string NormalizeTaxId(string taxId) =>
            new string(taxId.Where(char.IsDigit).ToArray());
    }
}
