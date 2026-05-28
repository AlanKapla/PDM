using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Users;
using Entities.Models.Users;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Users.UserDetails
{
    public class UserDetailsQueryHandler : IRequestHandler<UserDetailsQuery, UserDetailsWeb>
    {
        private readonly ICurrentUser currentUser;
        private readonly IReadRepository<User> userRepo;

        public UserDetailsQueryHandler(ICurrentUser currentUser, IReadRepository<User> userRepo)
        {
            this.currentUser = currentUser;
            this.userRepo = userRepo;
        }

        public async Task<UserDetailsWeb> Handle(UserDetailsQuery request, CancellationToken cancellationToken)
        {
            bool isActiveTenantAdmin = false;

            if (currentUser.IsAuthenticated && currentUser.ActiveTenantId.HasValue)
            {
                TenantCtxSnapshot? tenantSnapshot = await currentUser.GetActiveTenantSnapshotAsync(cancellationToken);
                isActiveTenantAdmin = tenantSnapshot?.IsAdmin ?? false;
            }

            User? user = await userRepo.GetById(currentUser.Id);

            return new UserDetailsWeb(
                currentUser.Id, 
                currentUser.FirstName, 
                currentUser.LastName, 
                currentUser.Email, 
                currentUser.ActiveTenantId,
                isActiveTenantAdmin,
                user?.PhoneNumber,
                user?.CompanyName,
                user?.TaxId,
                user?.Street,
                user?.City,
                user?.PostalCode,
                user?.Country);
        }
    }
}
