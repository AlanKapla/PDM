using Business.Interfaces.WebModels.Contractors;
using Entities.Models.Tenants;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Contractors.CreateContractor
{
    public sealed class CreateContractorCommandHandler : IRequestHandler<CreateContractorCommand, ContractorWeb>
    {
        private readonly IRepository<Contractor> contractorRepo;
        private readonly ILogger<CreateContractorCommandHandler> logger;

        public CreateContractorCommandHandler(
            IRepository<Contractor> contractorRepo,
            ILogger<CreateContractorCommandHandler> logger)
        {
            this.contractorRepo = contractorRepo;
            this.logger = logger;
        }

        public async Task<ContractorWeb> Handle(CreateContractorCommand request, CancellationToken cancellationToken)
        {
            Contractor contractor = new Contractor
            {
                TenantId = request.TenantId,
                Name = request.Name.Trim(),
                TaxId = request.TaxId,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Street = request.Street,
                City = request.City,
                PostalCode = request.PostalCode,
                Country = request.Country,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
            };

            await contractorRepo.Insert(contractor);
            await contractorRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Created Contractor {ContractorId} for tenant {TenantId}", contractor.Id, request.TenantId);

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
