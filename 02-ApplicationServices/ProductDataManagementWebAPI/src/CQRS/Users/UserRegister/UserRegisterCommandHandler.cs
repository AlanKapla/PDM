using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Users;
using Entities.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Repositiories.Repository.Interfaces;
using Services.Interfaces;

namespace CQRS.Users.UserRegister
{
    public class UserRegisterCommandHandler : IRequestHandler<UserRegisterCommand, UserRegisterWeb>
    {
        private readonly IReadRepository<User> userRepo;
        private readonly IJwtService jwt;
        private readonly IPasswordHasher passwordHasher;

        public UserRegisterCommandHandler(IReadRepository<User> userRepo, IJwtService jwt, IPasswordHasher passwordHasher)
        {
            this.userRepo = userRepo;
            this.jwt = jwt;
            this.passwordHasher = passwordHasher;
        }

        public async Task<UserRegisterWeb> Handle(UserRegisterCommand request, CancellationToken cancellationToken)
        {
            await EnsureEmailNotTaken(request.Email, cancellationToken).ConfigureAwait(false);

            var user = CreateUserFromRequest(request);

            HashPassword(user, request.Password);

            await InsertUserAsync(user).ConfigureAwait(false);

            _ = jwt.GenerateToken(user);

            return new UserRegisterWeb(user.Id, user.Email);
        }

        private async Task EnsureEmailNotTaken(string email, CancellationToken cancellationToken)
        {
            var existingUser = await userRepo.GetFirstBySearch(x => x.Email == email, cancellationToken).ConfigureAwait(false);
            if (existingUser != null)
            {
                throw new ConflictApiException(nameof(User), email);
            }
        }

        private static User CreateUserFromRequest(UserRegisterCommand request)
        {
            return new User
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName
            };
        }

        private void HashPassword(User user, string password)
        {
            var hashed = passwordHasher.Hash(password);
            user.PasswordHash = hashed;
        }

        private async Task InsertUserAsync(User user)
        {
            await userRepo.Insert(user).ConfigureAwait(false);
        }
    }
}