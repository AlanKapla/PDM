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
        private readonly IJwtService jwt;
        private readonly IPasswordHasher passwordHasher;

        public UserLoginCommandHandler(
            IReadRepository<User> userRepo,
            IReadRepository<UserSession> userSessionRepo,
            IJwtService jwt,
            IPasswordHasher passwordHasher)
        {
            this.userRepo = userRepo;
            this.userSessionRepo = userSessionRepo;
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

            if (string.IsNullOrEmpty(user?.PasswordHash))
            {
                throw new UnauthorizedApiExeption();
            }

            bool verifyResult = passwordHasher.Verify(request.Password, user.PasswordHash);

            if (verifyResult)
            {
                UserSession userSession = await CreateUserSession(user);

                return PrepareUserLoginWeb(user, userSession);
            }

            throw new UnauthorizedApiExeption();
        }

        private async Task<UserAuthWeb> GoogleLogin(UserLoginCommand request)
        {
            GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(request.ExternalToken, new GoogleJsonWebSignature.ValidationSettings());

            if (!payload.EmailVerified)
            {
                throw new UnauthorizedApiExeption();
            }

            User? user = await userRepo.GetFirstBySearch(x => x.Email == payload.Email);

            if (user != null)
            {
                UserSession userSession = await CreateUserSession(user);

                return PrepareUserLoginWeb(user, userSession);
            }

            throw new UnauthorizedApiExeption();
        }

        private UserAuthWeb PrepareUserLoginWeb(User user, UserSession userSession)
        {
            TokenDto token = jwt.GenerateToken(user, user.ActiveTenantId);

            return new UserAuthWeb(token.Token, token.ExpiredAt, userSession.RefreshToken, userSession.ExpiresAt);
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
    }
}