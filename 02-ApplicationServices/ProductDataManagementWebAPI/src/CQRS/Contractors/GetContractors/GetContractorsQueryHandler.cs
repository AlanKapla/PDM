using Business.Interfaces.WebModels.Contractors;
using Entities.Models.Tenants;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Contractors.GetContractors
{
    public sealed class GetContractorsQueryHandler : IRequestHandler<GetContractorsQuery, IEnumerable<ContractorWeb>>
    {
        private readonly IReadRepository<Contractor> contractorRepo;

        public GetContractorsQueryHandler(IReadRepository<Contractor> contractorRepo)
        {
            this.contractorRepo = contractorRepo;
        }

        public async Task<IEnumerable<ContractorWeb>> Handle(GetContractorsQuery request, CancellationToken cancellationToken)
        {
            IEnumerable<Contractor> contractors = await contractorRepo.GetBySearch(
                c => c.TenantId == request.TenantId && !c.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                string search = request.Search.Trim();
                contractors = contractors.Where(c =>
                    c.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || (c.TaxId != null && c.TaxId.Contains(search, StringComparison.OrdinalIgnoreCase))
                    || (c.City != null && c.City.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            return contractors
                .OrderBy(c => c.Name)
                .Select(MapToWeb)
                .ToList();
        }

        private static ContractorWeb MapToWeb(Contractor contractor) => new ContractorWeb
        {
            Id = contractor.Id,
            TenantId = contractor.TenantId,
            Name = contractor.Name,
            TaxId = contractor.TaxId,
            Email = contractor.Email,
            PhoneNumber = contractor.PhoneNumber,
            Street = contractor.Street,
            City = contractor.City,
            PostalCode = contractor.PostalCode,
            Country = contractor.Country,
            Notes = contractor.Notes,
            CreatedAt = contractor.CreatedAt,
            UpdatedAt = contractor.UpdatedAt,
        };
    }
}
