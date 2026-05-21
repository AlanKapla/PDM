using System.Text.Json;
using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services
{
    public class NotificationWorker : BackgroundService
    {
        private readonly IQueueStorageService queueStorage;
        private readonly ILogger<NotificationWorker> logger;
        private readonly INotificationDispatcher dispatcher;

        public NotificationWorker(IQueueStorageService queueStorage, ILogger<NotificationWorker> logger, INotificationDispatcher dispatcher)
        {
            this.queueStorage = queueStorage;
            this.logger = logger;
            this.dispatcher = dispatcher;
        }

        private const int MaxDequeueCount = 5;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await queueStorage.EnsureQueueAsync(QueueNames.NotificationSend, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    DequeuedMessage? message = await queueStorage.DequeueAsync(QueueNames.NotificationSend, cancellationToken: stoppingToken);
                    if (message is null)
                    {
                        // Krótszy delay (100ms zamiast 1s) - szybsza reakcja
                        await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
                        continue;
                    }

                    if (message.DequeueCount > MaxDequeueCount)
                    {
                        logger.LogError(
                            "Poison message detected after {Count} attempts. MessageId: {MessageId}. Deleting.",
                            message.DequeueCount, message.MessageId);
                        await queueStorage.DeleteMessageAsync(QueueNames.NotificationSend, message.MessageId, message.PopReceipt, stoppingToken);
                        continue;
                    }

                    logger.LogInformation("📩 Processing notification from queue: {MessageId}", message.MessageId);

                    NotificationPayloadDto? payload = JsonSerializer.Deserialize<NotificationPayloadDto>(message.Text, JsonOptions);

                    if (payload != null)
                    {
                        logger.LogInformation("🔔 Dispatching notification {NotificationId} to user {UserId} with unread count {UnreadCount}", 
                            payload.Notification.Id, payload.Notification.AzureAdB2CObjectId, payload.UnreadNotificationCounter);
                        await dispatcher.DispatchAsync(payload, stoppingToken);
                        logger.LogInformation("✅ Notification {NotificationId} dispatched successfully", payload.Notification.Id);
                    }
                    else
                    {
                        logger.LogWarning("Received invalid notification message: {MessageId}", message.MessageId);
                    }

                    await queueStorage.DeleteMessageAsync(QueueNames.NotificationSend, message.MessageId, message.PopReceipt, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Error processing notification queue message");
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
        }
    }
}
