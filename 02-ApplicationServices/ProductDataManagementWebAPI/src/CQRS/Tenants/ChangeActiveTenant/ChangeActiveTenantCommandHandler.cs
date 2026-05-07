using Entities.Models;
using Business.Interfaces.Model;
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
