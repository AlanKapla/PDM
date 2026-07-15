using Business.Implementation.Helpers;
using Business.Interfaces.Configurations;
using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Entities.Models.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Business.Implementation.Services
{
    public sealed class WelcomeEmailService : IWelcomeEmailService
    {
        private readonly IEmailSender emailSender;
        private readonly IOptions<FrontendSettings> frontendSettings;
        private readonly ILogger<WelcomeEmailService> logger;

        public WelcomeEmailService(
            IEmailSender emailSender,
            IOptions<FrontendSettings> frontendSettings,
            ILogger<WelcomeEmailService> logger)
        {
            this.emailSender = emailSender;
            this.frontendSettings = frontendSettings;
            this.logger = logger;
        }

        public async Task SendWelcomeEmailAsync(User user, CancellationToken cancellationToken = default)
        {
            string baseUrl = frontendSettings.Value.BaseUrl.TrimEnd('/');
            string homePath = frontendSettings.Value.HomePath.TrimStart('/');
            string appUrl = $"{baseUrl}/{homePath}";

            string firstName = string.IsNullOrWhiteSpace(user.FirstName)
                ? "Użytkowniku"
                : user.FirstName;

            string bodyText =
                "Twoje konto w Brickly zostało pomyślnie utworzone. " +
                "Możesz już zarządzać projektami budowlanymi, kosztorysami i harmonogramami prac w jednym miejscu.";

            string ctaLabel = "Przejdź do Brickly";

            string textBody =
                $"Witaj {firstName}! Twoje konto w Brickly zostało utworzone. " +
                $"Zaloguj się i rozpocznij pracę: {appUrl}";

            string htmlBody = EmailTemplateLoader.Load("welcome-email.html", new Dictionary<string, string>
            {
                { "firstName", firstName },
                { "appUrl", appUrl },
                { "bodyText", bodyText },
                { "ctaLabel", ctaLabel }
            });

            try
            {
                await emailSender.SendEmailAsync(new EmailMessageDto
                {
                    To = user.Email,
                    Subject = "Witaj w Brickly!",
                    TextBody = textBody,
                    HtmlBody = htmlBody
                }, cancellationToken);

                logger.LogInformation(
                    "Welcome email enqueued for user {UserId} ({Email})",
                    user.Id,
                    user.Email);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to send welcome email for user {UserId} ({Email})",
                    user.Id,
                    user.Email);
            }
        }
    }
}
