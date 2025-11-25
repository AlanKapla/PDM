using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.ChangeActiveTenant
{
    public class ChangeActiveTenantCommandHandler : IRequestHandler<ChangeActiveTenantCommand, ActiveTenantWeb>
    {
        private readonly IRepository<TenantPreferencesProfile> tenantPreferencesRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly ICurrentUser currentUser;

        public ChangeActiveTenantCommandHandler(
            IRepository<TenantPreferencesProfile> tenantPreferencesRepo,
            IRepository<TenantMember> tenantMemberRepo,
            ICurrentUser currentUser)
        {
            this.tenantPreferencesRepo = tenantPreferencesRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.currentUser = currentUser;
        }

        public async Task<ActiveTenantWeb> Handle(ChangeActiveTenantCommand request, CancellationToken cancellationToken)
        {
            // Walidacja przeniesiona do validatora

            // Pobierz lub utwórz profil preferencji
            var profile = await tenantPreferencesRepo.GetFirstBySearch(p => p.UserId == currentUser.Id);
            if (profile == null)
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

            return new ActiveTenantWeb(profile.ActiveTenantId);
        }
    }
}
