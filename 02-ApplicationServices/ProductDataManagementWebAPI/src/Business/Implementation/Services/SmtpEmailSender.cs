using Business.Interfaces.Configurations;
using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Business.Implementation.Services
{
    public sealed class SmtpEmailSender : IEmailTransport
    {
        private readonly SmtpSettings settings;
        private readonly ILogger<SmtpEmailSender> logger;

        public SmtpEmailSender(IOptions<SmtpSettings> options, ILogger<SmtpEmailSender> logger)
        {
            this.settings = options.Value;
            this.logger = logger;
        }

        public async Task SendEmailAsync(EmailMessageDto message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(message.To))
                throw new ValidationApiException("Recipient (To) is required.");

            if (string.IsNullOrWhiteSpace(message.Subject))
                throw new ValidationApiException("Subject is required.");

            if (string.IsNullOrWhiteSpace(message.HtmlBody) && string.IsNullOrWhiteSpace(message.TextBody))
                throw new ValidationApiException("Either HtmlBody or TextBody must be provided.");

            string senderAddress = message.From ?? settings.SenderAddress;
            if (string.IsNullOrWhiteSpace(senderAddress))
                throw new ValidationApiException("SenderAddress is not configured.");

            var mimeMessage = BuildMimeMessage(message, senderAddress);

            try
            {
                using var smtpClient = new SmtpClient();
                await smtpClient.ConnectAsync(settings.SmtpHost, settings.SmtpPort, SecureSocketOptions.StartTls, cancellationToken);
                await smtpClient.AuthenticateAsync(settings.SenderAddress, settings.AppPassword, cancellationToken);
                await smtpClient.SendAsync(mimeMessage, cancellationToken);
                await smtpClient.DisconnectAsync(quit: true, cancellationToken);

                logger.LogInformation("Email sent to {To} with subject {Subject}", message.To, message.Subject);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send email to {To} with subject {Subject}", message.To, message.Subject);
                throw;
            }
        }

        private MimeMessage BuildMimeMessage(EmailMessageDto message, string senderAddress)
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(settings.SenderName, senderAddress));
            mimeMessage.To.Add(new MailboxAddress(string.Empty, message.To));
            mimeMessage.Subject = message.Subject;

            var bodyBuilder = new BodyBuilder();
            if (!string.IsNullOrWhiteSpace(message.HtmlBody))
                bodyBuilder.HtmlBody = message.HtmlBody;
            if (!string.IsNullOrWhiteSpace(message.TextBody))
                bodyBuilder.TextBody = message.TextBody;

            mimeMessage.Body = bodyBuilder.ToMessageBody();
            return mimeMessage;
        }
    }
}
