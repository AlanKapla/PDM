using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Users.UserAuthStatus
{
    public class UserAuthStatusQueryHandler : IRequestHandler<UserAuthStatusQuery, UserAuthStatusWeb>
    {
        private readonly IReadRepository<User> userRepo;
        private readonly ICurrentUser currentUser;

        public UserAuthStatusQueryHandler(
            IReadRepository<User> userRepo,
            ICurrentUser currentUser)
        {
            this.userRepo = userRepo;
            this.currentUser = currentUser;
        }

        public async Task<UserAuthStatusWeb> Handle(UserAuthStatusQuery request, CancellationToken cancellationToken)
        {
            User? user = await userRepo.GetById(currentUser.Id);
            if (user == null)
            {
                throw new UnauthorizedApiException();
            }

            // Azure AD B2C is the only authentication method
            return new UserAuthStatusWeb(
                HasLocalAuth: false,
                HasGoogleAuth: false,
                IsHybridAuth: false
            );
        }
    }
}
