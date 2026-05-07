using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.Tenants;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.UpdateTenant
{
    public class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, TenantDetailsWeb>
    {
        private readonly IRepository<Tenant> tenantRepo;

        public UpdateTenantCommandHandler(IRepository<Tenant> tenantRepo)
        {
            this.tenantRepo = tenantRepo;
        }

        public async Task<TenantDetailsWeb> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
        {
            var tenant = await tenantRepo.GetFirstBySearch(t => t.Id == request.TenantId) 
                ?? throw new NotFoundApiException(nameof(Tenant), request.TenantId.ToString());

            tenant.Name = request.Name.Trim();
            await tenantRepo.Update(tenant);

            return new TenantDetailsWeb
            {
                Id = tenant.Id,
                Name = tenant.Name,
                CreatedAt = tenant.CreatedAt,
                IsActive = tenant.IsActive,
                RoleCode = RoleCodes.TenantAdmin
            };
        }
    }
}
