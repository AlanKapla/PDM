using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using Google.Apis.Auth;
using MediatR;
using Repositiories.Repository.Interfaces;
using Services.Interfaces;

namespace CQRS.Users.UserGoogleRegister
{
    public class UserGoogleRegisterCommandHandler : IRequestHandler<UserGoogleRegisterCommand, UserAuthWeb>
    {
        private readonly IReadRepository<User> userRepo;
        private readonly IReadRepository<UserSession> userSessionRepo;
        private readonly IJwtService jwt;

        public UserGoogleRegisterCommandHandler(
            IReadRepository<User> userRepo,
            IReadRepository<UserSession> userSessionRepo,
            IJwtService jwt)
        {
            this.userRepo = userRepo;
            this.userSessionRepo = userSessionRepo;
            this.jwt = jwt;
        }

        public async Task<UserAuthWeb> Handle(UserGoogleRegisterCommand request, CancellationToken cancellationToken)
        {
            GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(
                request.GoogleToken, 
                new GoogleJsonWebSignature.ValidationSettings());

            if (!payload.EmailVerified)
            {
                throw new ApiException(ApiExceptionReason.InvalidOperation, 
                    "Google email is not verified");
            }

            User? existingGoogleUser = await userRepo.GetFirstBySearch(x => x.ExternalId == payload.Subject);
            if (existingGoogleUser != null)
            {
                throw new ApiException(ApiExceptionReason.InvalidOperation, 
                    "User with this Google account already exists. Please log in instead.");
            }

            User? existingLocalUser = await userRepo.GetFirstBySearch(x => x.Email == payload.Email);
            
            if (existingLocalUser != null)
            {
                existingLocalUser.ExternalId = payload.Subject;
                
                await userRepo.Update(existingLocalUser);

                if (!existingLocalUser.IsActive)
                {
                    throw new ApiException(ApiExceptionReason.InvalidOperation, 
                        "Account is not activated. Please check your email for the activation link.");
                }

                UserSession linkedUserSession = await CreateUserSession(existingLocalUser);
                return PrepareUserAuthWeb(existingLocalUser, linkedUserSession);
            }

            User newUser = new User
            {
                Email = payload.Email,
                FirstName = payload.GivenName ?? string.Empty,
                LastName = payload.FamilyName ?? string.Empty,
                AuthProvider = AuthProvider.Google,
                ExternalId = payload.Subject,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await userRepo.Insert(newUser);

            UserSession userSession = await CreateUserSession(newUser);

            return PrepareUserAuthWeb(newUser, userSession);
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

        private UserAuthWeb PrepareUserAuthWeb(User user, UserSession userSession)
        {
            TokenDto token = jwt.GenerateToken(user, user.ActiveTenantId);

            return new UserAuthWeb(token.Token, token.ExpiredAt, userSession.RefreshToken, userSession.ExpiresAt);
        }
    }
}