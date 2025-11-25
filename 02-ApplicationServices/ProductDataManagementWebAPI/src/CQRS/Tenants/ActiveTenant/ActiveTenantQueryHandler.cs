using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Tenants;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Tenants.ActiveTenant
{
    public class ActiveTenantQueryHandler : IRequestHandler<ActiveTenantQuery, ActiveTenantWeb>
    {
        private readonly IRepository<TenantPreferencesProfile> tenantPreferencesRepo;
        private readonly ICurrentUser currentUser;

        public ActiveTenantQueryHandler(IRepository<TenantPreferencesProfile> tenantPreferencesRepo, ICurrentUser currentUser)
        {
            this.tenantPreferencesRepo = tenantPreferencesRepo;
            this.currentUser = currentUser;
        }

        public async Task<ActiveTenantWeb> Handle(ActiveTenantQuery request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated || currentUser.Id == Guid.Empty)
            {
                throw new UnauthorizedApiExeption();
            }

            var profile = await tenantPreferencesRepo.GetFirstBySearch(p => p.UserId == currentUser.Id);

            return new ActiveTenantWeb(profile?.ActiveTenantId);
        }
    }
}
