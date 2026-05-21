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
    }
}
