using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using Google.Apis.Auth;
using MediatR;
using Repositiories.Repository.Interfaces;
using Services.Interfaces;

namespace CQRS.Users.UserLogin
{
    public class UserLoginCommandHandler : IRequestHandler<UserLoginCommand, UserAuthWeb>
    {
        private readonly IReadRepository<User> userRepo;
        private readonly IReadRepository<UserSession> userSessionRepo;
        private readonly IReadRepository<TenantPreferencesProfile> tenantPrefsRepo;
        private readonly IJwtService jwt;
        private readonly IPasswordHasher passwordHasher;

        public UserLoginCommandHandler(
            IReadRepository<User> userRepo,
            IReadRepository<UserSession> userSessionRepo,
            IReadRepository<TenantPreferencesProfile> tenantPrefsRepo,
            IJwtService jwt,
            IPasswordHasher passwordHasher)
        {
            this.userRepo = userRepo;
            this.userSessionRepo = userSessionRepo;
            this.tenantPrefsRepo = tenantPrefsRepo;
            this.jwt = jwt;
            this.passwordHasher = passwordHasher;
        }

        public async Task<UserAuthWeb> Handle(UserLoginCommand request, CancellationToken cancellationToken)
        {
            Func<UserLoginCommand, Task<UserAuthWeb>> handler = request.Provider switch
            {
                LoginProvider.Local => LocalLogin,
                LoginProvider.Google => GoogleLogin,
                _ => _ => throw new ApiException(ApiExceptionReason.InvalidOperation, "Unsupported login provider"),
            };

            return await handler(request);
        }

        private async Task<UserAuthWeb> LocalLogin(UserLoginCommand request)
        {
            User? user = await userRepo.GetFirstBySearch(x => x.Email == request.Email);

            // Sprawdzenie czy użytkownik istnieje
            if (user == null)
            {
                throw new UnauthorizedApiException();
            }

            // HYBRID AUTH: Sprawdzenie czy użytkownik ma ustawione hasło (niezależnie od AuthProvider)
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                throw new ApiException(ApiExceptionReason.InvalidOperation, 
                    "Password not set for this account. Please set password or use Google login.");
            }

            if (!user.IsActive)
            {
                throw new ApiException(ApiExceptionReason.InvalidOperation, "Account is not activated. Please check your email for the activation link.");
            }

            bool verifyResult = passwordHasher.Verify(request.Password, user.PasswordHash);

            if (verifyResult)
            {
                UserSession userSession = await CreateUserSession(user);
                return await PrepareUserLoginWeb(user, userSession);
            }

            throw new UnauthorizedApiException();
        }

        private async Task<UserAuthWeb> GoogleLogin(UserLoginCommand request)
        {
            GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(request.ExternalToken, new GoogleJsonWebSignature.ValidationSettings());

            if (!payload.EmailVerified)
            {
                throw new UnauthorizedApiException();
            }

            // HYBRID AUTH: Sprawdź czy użytkownik istnieje z tym Google ID
            User? googleUser = await userRepo.GetFirstBySearch(x => x.ExternalId == payload.Subject);

            if (googleUser != null)
            {
                if (!googleUser.IsActive)
                {
                    throw new ApiException(ApiExceptionReason.InvalidOperation, "Account is not activated. Please check your email for the activation link.");
                }

                UserSession userSession = await CreateUserSession(googleUser);
                return await PrepareUserLoginWeb(googleUser, userSession);
            }

            // HYBRID AUTH: Sprawdź czy istnieje użytkownik z tym emailem (niezależnie od AuthProvider)
            User? existingUser = await userRepo.GetFirstBySearch(x => x.Email == payload.Email);

            if (existingUser != null)
            {
                // Połącz konto Google z istniejącym użytkownikiem
                existingUser.ExternalId = payload.Subject;
                
                // Aktualizuj AuthProvider tylko jeśli to pierwszy external provider
                if (existingUser.AuthProvider == AuthProvider.Local && string.IsNullOrEmpty(existingUser.ExternalId))
                {
                    existingUser.AuthProvider = AuthProvider.Google;
                }

                await userRepo.Update(existingUser);

                if (!existingUser.IsActive)
                {
                    throw new ApiException(ApiExceptionReason.InvalidOperation, "Account is not activated. Please check your email for the activation link.");
                }

                UserSession userSession = await CreateUserSession(existingUser);
                return await PrepareUserLoginWeb(existingUser, userSession);
            }

            // Jeśli użytkownik nie istnieje, zwróć błąd z informacją o konieczności rejestracji
            throw new ApiException(ApiExceptionReason.InvalidOperation, "User not found. Please register first using Google sign-up.");
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

        private async Task<UserAuthWeb> PrepareUserLoginWeb(User user, UserSession userSession)
        {
            // Pobierz ActiveTenantId z profilu użytkownika
            TenantPreferencesProfile? prefs = await tenantPrefsRepo.GetFirstBySearch(p => p.UserId == user.Id);
            Guid? activeTenantId = prefs?.ActiveTenantId;

            TokenDto token = jwt.GenerateToken(user, activeTenantId);

            return new UserAuthWeb(token.Token, token.ExpiredAt, userSession.RefreshToken, userSession.ExpiresAt);
        }
    }
}
