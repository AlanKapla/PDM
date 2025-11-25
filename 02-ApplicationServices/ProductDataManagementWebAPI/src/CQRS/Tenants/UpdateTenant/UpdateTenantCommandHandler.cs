using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using Entities.Enums;
using Entities.Models;
using MediatR;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.UpdateTenant
{
    public class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, TenantDetailsWeb>
    {
        private readonly IRepository<Tenant> tenantRepo;
        private readonly ICurrentUser currentUser;

        public UpdateTenantCommandHandler(
            IRepository<Tenant> tenantRepo,
            ICurrentUser currentUser)
        {
            this.tenantRepo = tenantRepo;
            this.currentUser = currentUser;
        }

        public async Task<TenantDetailsWeb> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
        {
            var tenant = await tenantRepo.GetFirstBySearch(t => t.Id == request.TenantId) ?? throw new NotFoundApiException(nameof(Tenant), request.TenantId.ToString());

            tenant.Name = request.Name.Trim();
            await tenantRepo.Update(tenant);

            return new TenantDetailsWeb
            {
                Id = tenant.Id,
                Name = tenant.Name,
                CreatedAt = tenant.CreatedAt,
                Role = TenantRole.Admin
            };
        }
    }
}
