using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.Contractors;
using Entities.Models.Tenants;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Contractors.UpdateContractor
{
    public sealed class UpdateContractorCommandHandler : IRequestHandler<UpdateContractorCommand, ContractorWeb>
    {
        private readonly IRepository<Contractor> contractorRepo;

        public UpdateContractorCommandHandler(IRepository<Contractor> contractorRepo)
        {
            this.contractorRepo = contractorRepo;
        }

        public async Task<ContractorWeb> Handle(UpdateContractorCommand request, CancellationToken cancellationToken)
        {
            Contractor? contractor = await contractorRepo.GetFirstBySearch(
                c => c.Id == request.Id
                     && c.TenantId == request.TenantId
                     && !c.IsDeleted);

            if (contractor is null)
            {
                throw new NotFoundApiException(nameof(Contractor), request.Id.ToString());
            }

            contractor.Name = request.Name.Trim();
            contractor.TaxId = request.TaxId;
            contractor.Email = request.Email;
            contractor.PhoneNumber = request.PhoneNumber;
            contractor.Street = request.Street;
            contractor.City = request.City;
            contractor.PostalCode = request.PostalCode;
            contractor.Country = request.Country;
            contractor.Notes = request.Notes;
            contractor.UpdatedAt = DateTime.UtcNow;

            await contractorRepo.Update(contractor);
            await contractorRepo.SaveChangesAsync(cancellationToken);

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
