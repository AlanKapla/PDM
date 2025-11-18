using Business.Implementation.Model;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Tenants;
using Entities.Enums;
using Entities.Models;
using MediatR;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.CreateTenant
{
    public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, TenantDetailsWeb>
    {
        private readonly IReadRepository<Tenant> tenantRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly ICurrentUser currentUser;

        public CreateTenantCommandHandler(
            IReadRepository<Tenant> tenantRepo,
            IRepository<TenantMember> tenantMemberRepo,
            IReadRepository<User> userRepo,
            ICurrentUser currentUser)
        {
            this.tenantRepo = tenantRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.userRepo = userRepo;
            this.currentUser = currentUser;
        }

        public async Task<TenantDetailsWeb> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
        {
            Tenant tenant = new Tenant
            {
                Name = request.Name
            };

            await tenantRepo.Insert(tenant);

            TenantMember ownerMember = new TenantMember
            {
                TenantId = tenant.Id,
                UserId = currentUser.Id,
                Role = TenantRole.Admin
            };

            await tenantMemberRepo.Insert(ownerMember);

            User existingUser = await userRepo.GetById(currentUser.Id) ?? throw new NotFoundApiException(nameof(User), currentUser.Id.ToString());
            
            existingUser.ActiveTenantId = tenant.Id;

            await userRepo.Update(existingUser);

            return new TenantDetailsWeb
            {
                Id = tenant.Id,
                Name = tenant.Name,
                CreatedAt = tenant.CreatedAt
            };
        }
    }
}
