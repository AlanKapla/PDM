using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Users;
using Entities.Models;
using MediatR;
using Repositiories.Repository.Interfaces; // added for IReadRepository
using Repositories.Repository.Interfaces; // existing for IRepository
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Business.Interfaces.Configurations;

namespace CQRS.Users.UserPasswordResetRequest
{
    public class UserPasswordResetRequestCommandHandler : IRequestHandler<UserPasswordResetRequestCommand, UserPasswordResetRequestWeb>
    {
        private readonly IReadRepository<User> userReadRepo;
        private readonly IRepository<UserPasswordReset> passwordResetRepo;
        private readonly IEmailSender emailSender;
        private readonly FrontendSettings frontend;

        public UserPasswordResetRequestCommandHandler(
            IReadRepository<User> userReadRepo,
            IRepository<UserPasswordReset> passwordResetRepo,
            IEmailSender emailSender,
            IOptions<FrontendSettings> frontendOptions)
        {
            this.userReadRepo = userReadRepo;
            this.passwordResetRepo = passwordResetRepo;
            this.emailSender = emailSender;
            this.frontend = frontendOptions.Value;
        }

        public async Task<UserPasswordResetRequestWeb> Handle(UserPasswordResetRequestCommand request, CancellationToken cancellationToken)
        {
            User? user = await userReadRepo.GetFirstBySearch(x => x.Email == request.Email);
            if (user == null)
            {
                // Nie ujawniamy czy email istnieje – zwracamy sukces, ale bez tworzenia rekordu
                return new UserPasswordResetRequestWeb(request.Email);
            }

            string token = GenerateSecureToken();
            DateTime expires = DateTime.UtcNow.AddHours(1);

            UserPasswordReset reset = new()
            {
                UserId = user.Id,
                Token = token,
                ExpiresAt = expires
            };

            await passwordResetRepo.Insert(reset);

            string resetLink = $"{frontend.BaseUrl.TrimEnd('/')}{frontend.ResetPasswordPath}?token={Uri.EscapeDataString(token)}";
            string subject = "Password reset request";
            string textBody = $"If you requested a password reset, use this token: {token}. It expires at {expires:u}.";
            string htmlBody = $"<p>If you requested a password reset, click the link below or use the provided token.</p><p><a href=\"{resetLink}\">Reset Password</a></p><p>Token: <strong>{token}</strong></p><p>Expires at: {expires:u}</p>";

            await emailSender.SendEmailAsync(new EmailMessage
            {
                To = user.Email,
                Subject = subject,
                TextBody = textBody,
                HtmlBody = htmlBody
            }, cancellationToken);

            return new UserPasswordResetRequestWeb(user.Email);
        }

        private static string GenerateSecureToken()
        {
            Span<byte> bytes = stackalloc byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }
    }
}
