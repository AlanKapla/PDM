using Business.Interfaces.Configurations;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace Business.Implementation.Services
{
    public class SendGridEmailSender : IEmailSender
    {
        private readonly SendGridClient client;
        private readonly SendGridSettings settings;
        private readonly ILogger<SendGridEmailSender> logger;

        public SendGridEmailSender(
            IOptions<EmailSettings> options,
            ILogger<SendGridEmailSender> logger)
        {
            this.settings = options.Value.SendGrid;
            this.client = new SendGridClient(options.Value.SendGrid.ApiKey);
            this.logger = logger;
        }

        public async Task SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(message.To))
            {
                throw new ValidationApiException("Recipient (To) is required.");
            }

            if (string.IsNullOrWhiteSpace(message.Subject))
            {
                throw new ValidationApiException("Subject is required.");
            }

            if (string.IsNullOrWhiteSpace(message.HtmlBody) && string.IsNullOrWhiteSpace(message.TextBody))
            {
                throw new ValidationApiException("Either HtmlBody or TextBody must be provided.");
            }

            string fromEmail = message.From ?? settings.DefaultFromEmail;

            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                throw new ValidationApiException("DefaultFromEmail is not configured.");
            }

            EmailAddress from = new(fromEmail, settings.DefaultFromName);
            EmailAddress to = new(message.To);

            SendGridMessage sgMessage = MailHelper.CreateSingleEmail(
                from,
                to,
                message.Subject,
                message.TextBody ?? string.Empty,
                message.HtmlBody ?? message.TextBody ?? string.Empty);

            Response response = await client.SendEmailAsync(sgMessage, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Body.ReadAsStringAsync(cancellationToken);
                logger.LogError(
                    "Failed to send email to {To}. StatusCode: {StatusCode}, Body: {Body}",
                    message.To,
                    response.StatusCode,
                    body);

                throw new InvalidOperationException($"Failed to send email. Status code: {response.StatusCode}");
            }

            logger.LogInformation("Email sent to {To} with subject {Subject}", message.To, message.Subject);
        }
    }
}
