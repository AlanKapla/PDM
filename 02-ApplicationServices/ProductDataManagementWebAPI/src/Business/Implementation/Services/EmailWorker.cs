using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Services;

namespace Business.Implementation.Services
{
    public sealed class EmailWorker : BackgroundService
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly IQueueStorageService queueStorageService;
        private readonly IEmailTransport emailSender;
        private readonly ILogger<EmailWorker> logger;

        public EmailWorker(
            IQueueStorageService queueStorageService,
            IEmailTransport emailSender,
            ILogger<EmailWorker> logger)
        {
            this.queueStorageService = queueStorageService;
            this.emailSender = emailSender;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await queueStorageService.EnsureQueueAsync(QueueNames.EmailSend, stoppingToken);
            logger.LogInformation("EmailWorker started. Listening on queue {Queue}", QueueNames.EmailSend);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    DequeuedMessage? msg = await queueStorageService.DequeueAsync(QueueNames.EmailSend, TimeSpan.FromMinutes(2), stoppingToken);

                    if (msg is null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                        continue;
                    }

                    EmailMessageDto? email;
                    try
                    {
                        email = JsonSerializer.Deserialize<EmailMessageDto>(msg.Text, JsonOptions);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to deserialize email message from queue {Queue}. DequeueCount={DequeueCount}", QueueNames.EmailSend, msg.DequeueCount);
                        continue;
                    }

                    if (email is null)
                    {
                        logger.LogError("Deserialized email message is null for queue {Queue}. DequeueCount={DequeueCount}", QueueNames.EmailSend, msg.DequeueCount);
                        continue;
                    }

                    try
                    {
                        await emailSender.SendEmailAsync(email, stoppingToken);

                        await queueStorageService.DeleteMessageAsync(QueueNames.EmailSend, msg.MessageId, msg.PopReceipt, stoppingToken);
                        logger.LogInformation("Email sent and message deleted from queue {Queue}. To={To}", QueueNames.EmailSend, email.To);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to send email. Message will be re-queued. Queue={Queue}, DequeueCount={DequeueCount}", QueueNames.EmailSend, msg.DequeueCount);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error in EmailWorker loop. Queue={Queue}", QueueNames.EmailSend);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            logger.LogInformation("EmailWorker stopping.");
        }
    }
}
