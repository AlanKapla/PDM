using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Entities.Enums;
using Entities.Models.ColdMails;
using Repositories.Repository.Interfaces;

namespace Business.Implementation.Services
{
    public sealed class EmailWorker : BackgroundService
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
        private const int MaxDequeueCount = 5;
        private const int MaxErrorMessageLength = 2000;

        private readonly IServiceProvider serviceProvider;
        private readonly IQueueStorageService queueStorageService;
        private readonly IEmailTransport emailSender;
        private readonly ILogger<EmailWorker> logger;

        public EmailWorker(
            IServiceProvider serviceProvider,
            IQueueStorageService queueStorageService,
            IEmailTransport emailSender,
            ILogger<EmailWorker> logger)
        {
            this.serviceProvider = serviceProvider;
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
                    DequeuedMessage? msg = await queueStorageService.DequeueAsync(
                        QueueNames.EmailSend,
                        TimeSpan.FromMinutes(2),
                        stoppingToken);

                    if (msg is null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                        continue;
                    }

                    EmailMessageDto? email = TryDeserialize(msg);
                    if (email is null)
                    {
                        continue;
                    }

                    if (msg.DequeueCount > MaxDequeueCount)
                    {
                        logger.LogError(
                            "Poison message detected after {Count} attempts. MessageId: {MessageId}. Deleting.",
                            msg.DequeueCount,
                            msg.MessageId);

                        await MarkColdMailFailedAsync(
                            email.ColdMailHistoryId,
                            $"Poison message after {msg.DequeueCount} send attempts.",
                            stoppingToken);

                        await queueStorageService.DeleteMessageAsync(
                            QueueNames.EmailSend,
                            msg.MessageId,
                            msg.PopReceipt,
                            stoppingToken);
                        continue;
                    }

                    try
                    {
                        await emailSender.SendEmailAsync(email, stoppingToken);

                        bool markedSent = await MarkColdMailSentAsync(
                            email.ColdMailHistoryId,
                            stoppingToken);

                        if (!markedSent)
                        {
                            logger.LogWarning(
                                "ColdMailHistory {HistoryId} not updated to Sent. Leaving message on queue for retry. To={To}",
                                email.ColdMailHistoryId,
                                email.To);
                            continue;
                        }

                        await queueStorageService.DeleteMessageAsync(
                            QueueNames.EmailSend,
                            msg.MessageId,
                            msg.PopReceipt,
                            stoppingToken);

                        logger.LogInformation(
                            "Email sent and message deleted from queue {Queue}. To={To}",
                            QueueNames.EmailSend,
                            email.To);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            ex,
                            "Failed to send email. Message will be re-queued. Queue={Queue}, DequeueCount={DequeueCount}",
                            QueueNames.EmailSend,
                            msg.DequeueCount);
                        throw;
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

        private EmailMessageDto? TryDeserialize(DequeuedMessage msg)
        {
            try
            {
                EmailMessageDto? email = JsonSerializer.Deserialize<EmailMessageDto>(msg.Text, JsonOptions);
                if (email is null)
                {
                    logger.LogError(
                        "Deserialized email message is null for queue {Queue}. DequeueCount={DequeueCount}",
                        QueueNames.EmailSend,
                        msg.DequeueCount);
                }

                return email;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to deserialize email message from queue {Queue}. DequeueCount={DequeueCount}",
                    QueueNames.EmailSend,
                    msg.DequeueCount);
                return null;
            }
        }

        private async Task<bool> MarkColdMailSentAsync(
            Guid? coldMailHistoryId,
            CancellationToken cancellationToken)
        {
            if (coldMailHistoryId is null)
            {
                return true;
            }

            return await UpdateColdMailStatusAsync(
                coldMailHistoryId.Value,
                ColdMailStatus.Sent,
                errorMessage: null,
                cancellationToken);
        }

        private async Task MarkColdMailFailedAsync(
            Guid? coldMailHistoryId,
            string errorMessage,
            CancellationToken cancellationToken)
        {
            if (coldMailHistoryId is null)
            {
                return;
            }

            await UpdateColdMailStatusAsync(
                coldMailHistoryId.Value,
                ColdMailStatus.Failed,
                TruncateErrorMessage(errorMessage),
                cancellationToken);
        }

        private async Task<bool> UpdateColdMailStatusAsync(
            Guid coldMailHistoryId,
            ColdMailStatus status,
            string? errorMessage,
            CancellationToken cancellationToken)
        {
            try
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                IRepository<ColdMailHistory> historyRepo =
                    scope.ServiceProvider.GetRequiredService<IRepository<ColdMailHistory>>();

                ColdMailHistory? history = await historyRepo.GetFirstBySearch(
                    h => h.Id == coldMailHistoryId);
                if (history is null)
                {
                    logger.LogWarning(
                        "ColdMailHistory {HistoryId} not found when updating status to {Status}",
                        coldMailHistoryId,
                        status);
                    return false;
                }

                history.Status = status;
                history.ErrorMessage = errorMessage;
                await historyRepo.Update(history);
                await historyRepo.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to update ColdMailHistory {HistoryId} to {Status}",
                    coldMailHistoryId,
                    status);
                return false;
            }
        }

        private static string TruncateErrorMessage(string message)
        {
            if (message.Length <= MaxErrorMessageLength)
            {
                return message;
            }

            return message[..MaxErrorMessageLength];
        }
    }
}
