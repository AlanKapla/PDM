using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Users;
using MediatR;

namespace CQRS.Users.UserDetails
{
    public class UserDetailsQueryHandler : IRequestHandler<UserDetailsQuery, UserDetailsWeb>
    {
        private readonly ICurrentUser currentUser;

        public UserDetailsQueryHandler(ICurrentUser currentUser)
        {
            this.currentUser = currentUser;
        }

        public Task<UserDetailsWeb> Handle(UserDetailsQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new UserDetailsWeb
            {
                Email = currentUser.Email,
                LastTenantId = currentUser.ActiveTenantId,
            });
        }
    }
}