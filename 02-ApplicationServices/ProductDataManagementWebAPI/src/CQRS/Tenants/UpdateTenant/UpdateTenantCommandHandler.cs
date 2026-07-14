using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.Tenants;
using Entities.Models.Tenants;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.UpdateTenant
{
    public sealed class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, TenantDetailsWeb>
    {
        private readonly IRepository<Tenant> tenantRepo;

        public UpdateTenantCommandHandler(IRepository<Tenant> tenantRepo)
        {
            this.tenantRepo = tenantRepo;
        }

        public async Task<TenantDetailsWeb> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
        {
            Tenant tenant = await tenantRepo.GetFirstBySearch(t => t.Id == request.TenantId)
                ?? throw new NotFoundApiException(nameof(Tenant), request.TenantId.ToString());

            tenant.Name = request.Name.Trim();
            await tenantRepo.Update(tenant);

            return new TenantDetailsWeb
            {
                Id = tenant.Id,
                Name = tenant.Name,
                CreatedAt = tenant.CreatedAt,
                IsActive = tenant.IsActive,
                IsAdmin = true
            };
        }
    }
}
