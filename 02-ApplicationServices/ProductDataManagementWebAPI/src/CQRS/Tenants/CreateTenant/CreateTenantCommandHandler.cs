using Business.Interfaces.Constants;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Tenants;
using Entities.Enums;
using Entities.Models;
using Entities.Models.Roles;
using Entities.Models.Subscriptions;
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
        private readonly IReadRepository<Role> roleRepo;
        private readonly IPermissionsVersionService permissionsVersionService;
        private readonly ICurrentUser currentUser;
        private readonly IReadRepository<SubscriptionPlanDefinition> planDefinitionRepo;
        private readonly IRepository<TenantSubscription> tenantSubscriptionRepo;

        public CreateTenantCommandHandler(
            IReadRepository<Tenant> tenantRepo,
            IRepository<TenantMember> tenantMemberRepo,
            IRepository<TenantPreferencesProfile> tenantPrefsRepo,
            IReadRepository<Role> roleRepo,
            IPermissionsVersionService permissionsVersionService,
            ICurrentUser currentUser,
            IReadRepository<SubscriptionPlanDefinition> planDefinitionRepo,
            IRepository<TenantSubscription> tenantSubscriptionRepo)
        {
            this.tenantRepo = tenantRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.tenantPrefsRepo = tenantPrefsRepo;
            this.roleRepo = roleRepo;
            this.permissionsVersionService = permissionsVersionService;
            this.currentUser = currentUser;
            this.planDefinitionRepo = planDefinitionRepo;
            this.tenantSubscriptionRepo = tenantSubscriptionRepo;
        }

        public async Task<TenantDetailsWeb> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
        {
            Tenant tenant = new Tenant
            {
                Name = request.Name
            };

            await tenantRepo.Insert(tenant);
            await tenantRepo.SaveChangesAsync(cancellationToken);

            // Utwórz domyślną subskrypcję Free dla nowego tenanta
            SubscriptionPlanDefinition? freePlan = await planDefinitionRepo.GetFirstBySearch(
                p => p.Plan == SubscriptionPlan.Free,
                cancellationToken);

            if (freePlan is not null)
            {
                TenantSubscription subscription = TenantSubscription.CreateDefault(tenant.Id, freePlan);
                subscription.IsFullAccess = true;
                await tenantSubscriptionRepo.Insert(subscription);
                await tenantSubscriptionRepo.SaveChangesAsync(cancellationToken);
            }

            Role? adminRole = await roleRepo.GetFirstBySearch(
                r => r.Scope == RoleScope.Tenant && r.Code == RoleCodes.TenantAdmin,
                cancellationToken);

            if (adminRole is null)
            {
                throw new NotFoundApiException(nameof(Role), RoleCodes.TenantAdmin);
            }

            TenantMember ownerMember = new TenantMember
            {
                TenantId = tenant.Id,
                UserId = currentUser.Id,
                RoleId = adminRole.Id
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
                RoleCode = RoleCodes.TenantAdmin
            };
        }
    }
}
