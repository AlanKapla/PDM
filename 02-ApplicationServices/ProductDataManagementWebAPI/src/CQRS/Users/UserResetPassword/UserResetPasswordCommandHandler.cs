using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Users;
using Entities.Models;
using MediatR;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Users.UserResetPassword
{
    public class UserResetPasswordCommandHandler : IRequestHandler<UserResetPasswordCommand, UserResetPasswordWeb>
    {
        private readonly IReadRepository<User> userRepo;
        private readonly IReadRepository<UserSession> userSessionRepo;
        private readonly IRepository<UserPasswordReset> passwordResetRepo;
        private readonly IPasswordHasher passwordHasher;

        public UserResetPasswordCommandHandler(
            IReadRepository<User> userRepo,
            IReadRepository<UserSession> userSessionRepo,
            IRepository<UserPasswordReset> passwordResetRepo,
            IPasswordHasher passwordHasher)
        {
            this.userRepo = userRepo;
            this.passwordHasher = passwordHasher;
            this.userSessionRepo = userSessionRepo;
            this.passwordResetRepo = passwordResetRepo;
        }

        public async Task<UserResetPasswordWeb> Handle(UserResetPasswordCommand request, CancellationToken cancellationToken)
        {
            UserPasswordReset? reset = await passwordResetRepo.GetFirstBySearch(r => r.Token == request.Token);

            if (reset == null || reset.ExpiresAt < DateTime.UtcNow || reset.IsUsed)
            {
                throw new ValidationApiException("Invalid or expired token.");
            }

            User? user = await userRepo.GetById(reset.UserId) ?? throw new NotFoundApiException(nameof(User), reset.UserId.ToString());

            user.PasswordHash = passwordHasher.Hash(request.Password);
            reset.UsedAt = DateTime.UtcNow;

            await userRepo.Update(user);
            await passwordResetRepo.Update(reset);

            UserSession[] userSessions = (await userSessionRepo.GetBySearch(x => x.UserId == user.Id)).ToArray();
            foreach (UserSession session in userSessions)
            {
                session.IsRevoked = true;
                await userSessionRepo.Update(session);
            }

            return new UserResetPasswordWeb(user.Id, user.Email);
        }
    }
}