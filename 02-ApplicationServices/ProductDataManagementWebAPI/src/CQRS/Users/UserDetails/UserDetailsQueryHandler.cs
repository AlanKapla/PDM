using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Users;
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
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;

namespace CQRS.Users.UserDetails
{
    public class UserDetailsQueryHandler : IRequestHandler<UserDetailsQuery, UserDetailsWeb>
    {
        private readonly ICurrentUser currentUser;

        public UserDetailsQueryHandler(ICurrentUser currentUser)
        {
            this.currentUser = currentUser;
        }

        public async Task<UserDetailsWeb> Handle(UserDetailsQuery request, CancellationToken cancellationToken)
        {
            var activeTenantPermissions = new HashSet<string>();

            if (currentUser.IsAuthenticated && currentUser.ActiveTenantId.HasValue)
            {
                // Get active tenant permissions only
                var tenantSnapshot = await currentUser.GetActiveTenantSnapshotAsync(cancellationToken);
                if (tenantSnapshot != null)
                {
                    activeTenantPermissions = tenantSnapshot.TenantPermissionCodes;
                }
            }

            return new UserDetailsWeb(
                currentUser.Id, 
                currentUser.FirstName, 
                currentUser.LastName, 
                currentUser.Email, 
                currentUser.ActiveTenantId,
                activeTenantPermissions);
        }
    }
}
