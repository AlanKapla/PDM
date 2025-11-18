using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositiories.Repository.Interfaces;
using Services.Interfaces;

namespace CQRS.Users.UserRefresh
{
    public class UserRefreshQueryHandler : IRequestHandler<UserRefreshQuery, UserAuthWeb>
    {
        private readonly IJwtService jwtService;
        private readonly IReadRepository<UserSession> userSessionRepo;

        public UserRefreshQueryHandler(IJwtService jwtService, IReadRepository<UserSession> userSessionRepo)
        {
            this.jwtService = jwtService;
            this.userSessionRepo = userSessionRepo;
        }

        public async Task<UserAuthWeb> Handle(UserRefreshQuery request, CancellationToken cancellationToken)
        {
            UserSession? session = await userSessionRepo.GetFirstBySearch(
                s => s.RefreshToken == request.RefreshToken && s.ExpiresAt > DateTime.UtcNow && !s.IsRevoked,
                cancellationToken,
                q => q.Include(s => s.User)
            ) ?? throw new UnauthorizedApiExeption();

            User user = session.User ?? throw new UnauthorizedApiExeption();

            if (!user.IsActive)
            {
                throw new UnauthorizedApiExeption();
            }

            TokenDto token = jwtService.GenerateToken(user, user.ActiveTenantId);

            return new UserAuthWeb(token.Token, token.ExpiredAt, session.RefreshToken, session.ExpiresAt);
        }
    }
}