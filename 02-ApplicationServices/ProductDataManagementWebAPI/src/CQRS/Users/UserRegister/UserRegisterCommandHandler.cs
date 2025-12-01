using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Users;
using Entities.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Repositiories.Repository.Interfaces;
using Services.Interfaces;
using Repositories.Repository.Interfaces;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Business.Interfaces.Configurations;
using Business.Interfaces.DTO;

namespace CQRS.Users.UserRegister
{
    public class UserRegisterCommandHandler : IRequestHandler<UserRegisterCommand, UserRegisterWeb>
    {
        private readonly IReadRepository<User> userRepo;
        private readonly IRepository<UserActivation> activationRepo;
        private readonly IJwtService jwt;
        private readonly IPasswordHasher passwordHasher;
        private readonly IEmailSender emailSender;
        private readonly FrontendSettings frontend;
        private readonly ITokenGenerator tokenGenerator;

        public UserRegisterCommandHandler(IReadRepository<User> userRepo, IRepository<UserActivation> activationRepo, IJwtService jwt, IPasswordHasher passwordHasher, IEmailSender emailSender, IOptions<FrontendSettings> frontendOptions, ITokenGenerator tokenGenerator)
        {
            this.userRepo = userRepo;
            this.activationRepo = activationRepo;
            this.jwt = jwt;
            this.passwordHasher = passwordHasher;
            this.emailSender = emailSender;
            this.frontend = frontendOptions.Value;
            this.tokenGenerator = tokenGenerator;
        }

        public async Task<UserRegisterWeb> Handle(UserRegisterCommand request, CancellationToken cancellationToken)
        {
            await EnsureEmailNotTaken(request.Email, cancellationToken).ConfigureAwait(false);

            var user = CreateUserFromRequest(request);

            HashPassword(user, request.Password);

            await InsertUserAsync(user).ConfigureAwait(false);

            _ = jwt.GenerateToken(user);

            // Create activation token
            string token = tokenGenerator.GenerateToken();
            UserActivation activation = new()
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
            await activationRepo.Insert(activation);


            string activationLink = $"{frontend.BaseUrl.TrimEnd('/')}{frontend.ActivationPath}?token={Uri.EscapeDataString(token)}";
            await emailSender.SendEmailAsync(new EmailMessageDto
            {
                To = user.Email,
                Subject = "Activate your account",
                TextBody = $"Welcome {user.FirstName}! Activate your account: {activationLink}",
                HtmlBody = $"<p>Welcome {user.FirstName}!</p><p>Click the link to activate your account:</p><p><a href=\"{activationLink}\">Activate Account</a></p><p>This link expires in 24 hours.</p>"
            }, cancellationToken);

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