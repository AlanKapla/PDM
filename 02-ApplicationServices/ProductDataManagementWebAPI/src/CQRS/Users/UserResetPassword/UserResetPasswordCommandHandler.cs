using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Users;
using Entities.Models;
using MediatR;
using Repositiories.Repository.Interfaces;

namespace CQRS.Users.UserResetPassword
{
    public class UserResetPasswordCommandHandler : IRequestHandler<UserResetPasswordCommand, UserResetPasswordWeb>
    {
        private readonly IReadRepository<User> userRepo;
        private readonly IReadRepository<UserSession> userSessionRepo;
        private readonly IPasswordHasher passwordHasher;

        public UserResetPasswordCommandHandler(IReadRepository<User> userRepo, IReadRepository<UserSession> userSessionRepo, IPasswordHasher passwordHasher)
        {
            this.userRepo = userRepo;
            this.passwordHasher = passwordHasher;
            this.userSessionRepo = userSessionRepo;
        }

        public async Task<UserResetPasswordWeb> Handle(UserResetPasswordCommand request, CancellationToken cancellationToken)
        {
            User? user = await userRepo.GetFirstBySearch(x => x.Email == request.Email)
                ?? throw new NotFoundApiException(nameof(User), request.Email);

            user.PasswordHash = passwordHasher.Hash(request.Password);

            UserSession[] userSessions = (await userSessionRepo.GetBySearch(x => x.UserId == user.Id)).ToArray();

            await userRepo.Update(user);

            foreach (UserSession session in userSessions)
            {
                session.IsRevoked = true;
                await userSessionRepo.Update(session);
            }

            UserResetPasswordWeb userResetPasswordWeb = new(user.Id, user.Email);

            return userResetPasswordWeb;
        }
    }
}