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
        private readonly IReadRepository<TenantPreferencesProfile> tenantPrefsRepo;

        public UserRefreshQueryHandler(
            IJwtService jwtService, 
            IReadRepository<UserSession> userSessionRepo,
            IReadRepository<TenantPreferencesProfile> tenantPrefsRepo)
        {
            this.jwtService = jwtService;
            this.userSessionRepo = userSessionRepo;
            this.tenantPrefsRepo = tenantPrefsRepo;
        }

        public async Task<UserAuthWeb> Handle(UserRefreshQuery request, CancellationToken cancellationToken)
        {
            UserSession? session = await userSessionRepo.GetFirstBySearch(
                s => s.RefreshToken == request.RefreshToken && s.ExpiresAt > DateTime.UtcNow && !s.IsRevoked,
                cancellationToken,
                q => q.Include(s => s.User)
            ) ?? throw new UnauthorizedApiException();

            User user = session.User ?? throw new UnauthorizedApiException();

            if (!user.IsActive)
            {
                throw new UnauthorizedApiException();
            }

            // Pobierz ActiveTenantId z profilu użytkownika
            TenantPreferencesProfile? prefs = await tenantPrefsRepo.GetFirstBySearch(p => p.UserId == user.Id);
            Guid? activeTenantId = prefs?.ActiveTenantId;

            TokenDto token = jwtService.GenerateToken(user, activeTenantId);

            return new UserAuthWeb(token.Token, token.ExpiredAt, session.RefreshToken, session.ExpiresAt);
        }
    }
}
