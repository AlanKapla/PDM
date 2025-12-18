using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Business.Implementation.Services
{
    /// <summary>
    /// Application-facing email sender that enqueues messages to Azure Queue.
    /// </summary>
    public sealed class QueuedEmailSender : IEmailSender
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private readonly IQueueStorageService queueStorageService;
        private readonly ILogger<QueuedEmailSender> logger;

        public QueuedEmailSender(IQueueStorageService queueStorageService, ILogger<QueuedEmailSender> logger)
        {
            this.queueStorageService = queueStorageService;
            this.logger = logger;
        }

        public async Task SendEmailAsync(EmailMessageDto message, CancellationToken cancellationToken = default)
        {
            await queueStorageService.EnsureQueueAsync(QueueNames.EmailSend, cancellationToken);

            string payload = JsonSerializer.Serialize(message, JsonOptions);
            await queueStorageService.EnqueueAsync(QueueNames.EmailSend, payload, cancellationToken: cancellationToken);

            logger.LogInformation("Enqueued email to {To} with subject {Subject}", message.To, message.Subject);
        }
    }
}
