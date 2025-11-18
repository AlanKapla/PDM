using Business.Interfaces.WebModels.Users;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositiories.Repository.Interfaces;

namespace CQRS.Users.UserLogout
{
    public class UserLogoutCommandHandler : IRequestHandler<UserLogoutCommand, UserLogoutWeb>
    {
        private readonly IReadRepository<UserSession> userSessionRepo;

        public UserLogoutCommandHandler(IReadRepository<UserSession> userSessionRepo)
        {
            this.userSessionRepo = userSessionRepo;
        }

        public async Task<UserLogoutWeb> Handle(UserLogoutCommand request, CancellationToken cancellationToken)
        {
            UserSession? session = await userSessionRepo.GetFirstBySearch(
                 s => s.RefreshToken == request.RefreshToken && s.ExpiresAt > DateTime.UtcNow && !s.IsRevoked,
                 cancellationToken);

            if (session == null)
            {
                return new UserLogoutWeb(false, "Session not found or already logged out.");
            }

            session.IsRevoked = true;

            await userSessionRepo.Update(session);

            return new UserLogoutWeb(true, "User logged out successfully.");
        }
    }
}