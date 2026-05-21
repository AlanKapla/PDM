using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.Contractors;
using Entities.Models.Tenants;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Contractors.GetContractor
{
    public sealed class GetContractorQueryHandler : IRequestHandler<GetContractorQuery, ContractorWeb>
    {
        private readonly IReadRepository<Contractor> contractorRepo;

        public GetContractorQueryHandler(IReadRepository<Contractor> contractorRepo)
        {
            this.contractorRepo = contractorRepo;
        }

        public async Task<ContractorWeb> Handle(GetContractorQuery request, CancellationToken cancellationToken)
        {
            Contractor? contractor = await contractorRepo.GetFirstBySearch(
                c => c.Id == request.ContractorId
                     && c.TenantId == request.TenantId
                     && !c.IsDeleted,
                cancellationToken);

            if (contractor is null)
            {
                throw new NotFoundApiException(nameof(Contractor), request.ContractorId.ToString());
            }

            return MapToWeb(contractor);
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
