using System.Text.Json;
using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services
{
    public class NotificationMarkAsReadWorker : BackgroundService
    {
        private readonly IQueueStorageService queueStorage;
        private readonly ILogger<NotificationMarkAsReadWorker> logger;
        private readonly INotificationMarkAsReadDispatcher dispatcher;

        public NotificationMarkAsReadWorker(
            IQueueStorageService queueStorage, 
            ILogger<NotificationMarkAsReadWorker> logger, 
            INotificationMarkAsReadDispatcher dispatcher)
        {
            this.queueStorage = queueStorage;
            this.logger = logger;
            this.dispatcher = dispatcher;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await queueStorage.EnsureQueueAsync(QueueNames.NotificationMarkAsRead, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    DequeuedMessage? message = await queueStorage.DequeueAsync(QueueNames.NotificationMarkAsRead, cancellationToken: stoppingToken);
                    if (message is null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                        continue;
                    }

                    JsonSerializerOptions jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    NotificationMarkAsReadDto? notificationMarkAsRead = JsonSerializer.Deserialize<NotificationMarkAsReadDto>(message.Text, jsonOptions);

                    if (notificationMarkAsRead != null)
                    {
                        await dispatcher.DispatchAsync(notificationMarkAsRead, stoppingToken);
                    }
                    else
                    {
                        logger.LogWarning("Received invalid notification mark as read message: {MessageId}", message.MessageId);
                    }

                    await queueStorage.DeleteMessageAsync(QueueNames.NotificationMarkAsRead, message.MessageId, message.PopReceipt, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Error processing notification mark as read queue message");
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
        }
    }
}
