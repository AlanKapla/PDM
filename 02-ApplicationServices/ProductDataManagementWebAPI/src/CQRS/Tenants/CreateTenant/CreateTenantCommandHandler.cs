using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
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
        private readonly IRepository<TenantPreferencesProfile> tenantPrefsRepo;
        private readonly ICurrentUser currentUser;

        public CreateTenantCommandHandler(
            IReadRepository<Tenant> tenantRepo,
            IRepository<TenantMember> tenantMemberRepo,
            IRepository<TenantPreferencesProfile> tenantPrefsRepo,
            ICurrentUser currentUser)
        {
            this.tenantRepo = tenantRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.tenantPrefsRepo = tenantPrefsRepo;
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

            // Ustaw aktywny tenant w profilu użytkownika
            TenantPreferencesProfile? profile = await tenantPrefsRepo.GetFirstBySearch(p => p.UserId == currentUser.Id);
            
            if (profile == null)
            {
                profile = new TenantPreferencesProfile
                {
                    UserId = currentUser.Id,
                    ActiveTenantId = tenant.Id
                };
                await tenantPrefsRepo.Insert(profile);
            }
            else
            {
                profile.ActiveTenantId = tenant.Id;
                await tenantPrefsRepo.Update(profile);
            }

            return new TenantDetailsWeb
            {
                Id = tenant.Id,
                Name = tenant.Name,
                CreatedAt = tenant.CreatedAt,
                IsActive = tenant.IsActive,
                Role = TenantRole.Admin
            };
        }
    }
}
