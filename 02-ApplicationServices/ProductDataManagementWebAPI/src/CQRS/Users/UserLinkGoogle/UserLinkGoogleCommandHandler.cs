using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using Google.Apis.Auth;
using MediatR;
using Repositiories.Repository.Interfaces;
using Services.Interfaces;

namespace CQRS.Users.UserLinkGoogle
{
    public class UserLinkGoogleCommandHandler : IRequestHandler<UserLinkGoogleCommand, UserAuthWeb>
    {
        private readonly IReadRepository<User> userRepo;
        private readonly IReadRepository<UserSession> userSessionRepo;
        private readonly IReadRepository<TenantPreferencesProfile> tenantPrefsRepo;
        private readonly IJwtService jwt;
        private readonly ICurrentUser currentUser;

        public UserLinkGoogleCommandHandler(
            IReadRepository<User> userRepo,
            IReadRepository<UserSession> userSessionRepo,
            IReadRepository<TenantPreferencesProfile> tenantPrefsRepo,
            IJwtService jwt,
            ICurrentUser currentUser)
        {
            this.userRepo = userRepo;
            this.userSessionRepo = userSessionRepo;
            this.tenantPrefsRepo = tenantPrefsRepo;
            this.jwt = jwt;
            this.currentUser = currentUser;
        }

        public async Task<UserAuthWeb> Handle(UserLinkGoogleCommand request, CancellationToken cancellationToken)
        {
            // Pobranie aktualnie zalogowanego użytkownika
            User? user = await userRepo.GetById(currentUser.Id);
            if (user == null)
            {
                throw new UnauthorizedApiException();
            }

            // Walidacja tokenu Google
            GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(
                request.GoogleToken, 
                new GoogleJsonWebSignature.ValidationSettings());

            if (!payload.EmailVerified)
            {
                throw new ApiException(ApiExceptionReason.InvalidOperation, 
                    "Google email is not verified");
            }

            // Sprawdzenie czy email się zgadza
            if (user.Email != payload.Email)
            {
                throw new ApiException(ApiExceptionReason.InvalidOperation, 
                    "Google account email must match your current account email");
            }

            // Sprawdzenie czy Google account już jest połączone z innym użytkownikiem
            User? existingGoogleUser = await userRepo.GetFirstBySearch(x => 
                x.ExternalId == payload.Subject && x.Id != user.Id);
                
            if (existingGoogleUser != null)
            {
                throw new ApiException(ApiExceptionReason.InvalidOperation, 
                    "This Google account is already linked to another user");
            }

            // Sprawdzenie czy użytkownik już ma połączone Google
            if (!string.IsNullOrEmpty(user.ExternalId))
            {
                throw new ApiException(ApiExceptionReason.InvalidOperation, 
                    "Google account is already linked to this user");
            }

            // Linkowanie Google z kontem
            user.ExternalId = payload.Subject;
            
            await userRepo.Update(user);

            // Tworzenie nowej sesji
            UserSession userSession = await CreateUserSession(user);

            return await PrepareUserAuthWeb(user, userSession);
        }

        private async Task<UserSession> CreateUserSession(User user)
        {
            string refreshToken = Guid.NewGuid().ToString();

            UserSession userSession = new UserSession
            {
                UserId = user.Id,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(90),
                IsRevoked = false
            };

            await userSessionRepo.Insert(userSession);
            return userSession;
        }

        private async Task<UserAuthWeb> PrepareUserAuthWeb(User user, UserSession userSession)
        {
            // Pobierz ActiveTenantId z profilu użytkownika
            TenantPreferencesProfile? prefs = await tenantPrefsRepo.GetFirstBySearch(p => p.UserId == user.Id);
            Guid? activeTenantId = prefs?.ActiveTenantId;

            TokenDto token = jwt.GenerateToken(user, activeTenantId);

            return new UserAuthWeb(token.Token, token.ExpiredAt, userSession.RefreshToken, userSession.ExpiresAt);
        }
    }
}
