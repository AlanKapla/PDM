using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.ChangeActiveTenant
{
    public sealed class ChangeActiveTenantCommandHandler : IRequestHandler<ChangeActiveTenantCommand, ActiveTenantWeb>
    {
        private readonly IRepository<TenantPreferencesProfile> tenantPreferencesRepo;
        private readonly ICurrentUser currentUser;

        public ChangeActiveTenantCommandHandler(
            IRepository<TenantPreferencesProfile> tenantPreferencesRepo,
            ICurrentUser currentUser)
        {
            this.tenantPreferencesRepo = tenantPreferencesRepo;
            this.currentUser = currentUser;
        }

        public async Task<ActiveTenantWeb> Handle(ChangeActiveTenantCommand request, CancellationToken cancellationToken)
        {
            TenantPreferencesProfile? profile = await tenantPreferencesRepo.GetFirstBySearch(p => p.UserId == currentUser.Id);
            if (profile is null)
            {
                profile = new TenantPreferencesProfile
                {
                    UserId = currentUser.Id,
                    ActiveTenantId = request.TenantId
                };
                await tenantPreferencesRepo.Insert(profile);
            }
            else
            {
                profile.ActiveTenantId = request.TenantId;
                await tenantPreferencesRepo.Update(profile);
            }

            return new ActiveTenantWeb { ActiveTenantId = profile.ActiveTenantId };
        }
    }
}
