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
using Polly;
using Polly.Retry;
using Business.Interfaces.DTO;

namespace Business.Implementation.Services
{
    public class SendGridEmailSender : IEmailTransport
    {
        private readonly SendGridClient client;
        private readonly SendGridSettings settings;
        private readonly ILogger<SendGridEmailSender> logger;
        private readonly AsyncRetryPolicy<Response> retryPolicy;

        public SendGridEmailSender(
            IOptions<EmailSettings> options,
            ILogger<SendGridEmailSender> logger)
        {
            this.settings = options.Value.SendGrid;
            this.client = new SendGridClient(options.Value.SendGrid.ApiKey);
            this.logger = logger;

            retryPolicy = Policy
                .Handle<Exception>()
                .OrResult<Response>(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: (attempt, ctx) => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (outcome, timespan, attempt, ctx) =>
                    {
                        if (outcome.Exception != null)
                        {
                            logger.LogWarning(outcome.Exception,
                                "Attempt {Attempt} to send email failed. Retrying in {Delay}s", attempt, timespan.TotalSeconds);
                        }
                        else
                        {
                            logger.LogWarning(
                                "Attempt {Attempt} to send email failed with status {StatusCode}. Retrying in {Delay}s",
                                attempt,
                                outcome.Result.StatusCode,
                                timespan.TotalSeconds);
                        }
                    });
        }

        public async Task SendEmailAsync(EmailMessageDto message, CancellationToken cancellationToken = default)
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

            Response response = await retryPolicy.ExecuteAsync(ct => client.SendEmailAsync(sgMessage, ct), cancellationToken);

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
