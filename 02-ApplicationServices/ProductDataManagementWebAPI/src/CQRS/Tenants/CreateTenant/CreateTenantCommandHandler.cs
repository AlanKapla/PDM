using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Tenants;
using Entities.Models;
using Entities.Models.Tenants;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.CreateTenant
{
    public sealed class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, TenantDetailsWeb>
    {
        private readonly IReadRepository<Tenant> tenantRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly IRepository<TenantPreferencesProfile> tenantPrefsRepo;
        private readonly IPermissionsVersionService permissionsVersionService;
        private readonly ICurrentUser currentUser;

        public CreateTenantCommandHandler(
            IReadRepository<Tenant> tenantRepo,
            IRepository<TenantMember> tenantMemberRepo,
            IRepository<TenantPreferencesProfile> tenantPrefsRepo,
            IPermissionsVersionService permissionsVersionService,
            ICurrentUser currentUser)
        {
            this.tenantRepo = tenantRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.tenantPrefsRepo = tenantPrefsRepo;
            this.permissionsVersionService = permissionsVersionService;
            this.currentUser = currentUser;
        }

        public async Task<TenantDetailsWeb> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
        {
            Tenant tenant = new Tenant
            {
                Name = request.Name
            };

            await tenantRepo.Insert(tenant);
            await tenantRepo.SaveChangesAsync(cancellationToken);

            TenantMember ownerMember = new TenantMember
            {
                TenantId = tenant.Id,
                UserId = currentUser.Id,
                IsAdmin = true
            };

            await tenantMemberRepo.Insert(ownerMember);
            await tenantMemberRepo.SaveChangesAsync(cancellationToken);

            await permissionsVersionService.BumpVersionAsync(currentUser.Id, cancellationToken);

            TenantPreferencesProfile? profile = await tenantPrefsRepo.GetFirstBySearch(p => p.UserId == currentUser.Id);

            if (profile is null)
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
                IsAdmin = true
            };
        }
    }
}
