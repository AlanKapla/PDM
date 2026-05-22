using Business.Interfaces.Exceptions;
using Entities.Models.Tenants;
using MediatR;
using Microsoft.Extensions.Logging;
using Repositories.Repository.Interfaces;

namespace CQRS.Contractors.DeleteContractor
{
    public sealed class DeleteContractorCommandHandler : IRequestHandler<DeleteContractorCommand, Unit>
    {
        private readonly IRepository<Contractor> contractorRepo;
        private readonly ILogger<DeleteContractorCommandHandler> logger;

        public DeleteContractorCommandHandler(
            IRepository<Contractor> contractorRepo,
            ILogger<DeleteContractorCommandHandler> logger)
        {
            this.contractorRepo = contractorRepo;
            this.logger = logger;
        }

        public async Task<Unit> Handle(DeleteContractorCommand request, CancellationToken cancellationToken)
        {
            Contractor? contractor = await contractorRepo.GetFirstBySearch(
                c => c.Id == request.Id
                     && c.TenantId == request.TenantId
                     && !c.IsDeleted);

            if (contractor is null)
            {
                throw new NotFoundApiException(nameof(Contractor), request.Id.ToString());
            }

            contractor.IsDeleted = true;
            contractor.DeletedAt = DateTime.UtcNow;

            await contractorRepo.Update(contractor);
            await contractorRepo.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Deleted Contractor {ContractorId} for tenant {TenantId}", contractor.Id, request.TenantId);

            return Unit.Value;
        }
    }
}
