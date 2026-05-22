using System.Text.Json;
using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Business.Implementation.Services
{
    public class MessageWorker : BackgroundService
    {
        private readonly IQueueStorageService queueStorage;
        private readonly ILogger<MessageWorker> logger;
        private readonly IMessageDispatcher dispatcher;

        public MessageWorker(IQueueStorageService queueStorage, ILogger<MessageWorker> logger, IMessageDispatcher dispatcher)
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
            await queueStorage.EnsureQueueAsync(QueueNames.MessageSend, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    DequeuedMessage? message = await queueStorage.DequeueAsync(QueueNames.MessageSend, cancellationToken: stoppingToken);
                    if (message is null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                        continue;
                    }

                    if (message.DequeueCount > MaxDequeueCount)
                    {
                        logger.LogError(
                            "Poison message detected after {Count} attempts. MessageId: {MessageId}. Deleting.",
                            message.DequeueCount, message.MessageId);
                        await queueStorage.DeleteMessageAsync(QueueNames.MessageSend, message.MessageId, message.PopReceipt, stoppingToken);
                        continue;
                    }

                    MessageDto? chatMessage = JsonSerializer.Deserialize<MessageDto>(message.Text, JsonOptions);

                    if (chatMessage != null)
                    {
                        await dispatcher.DispatchAsync(chatMessage, stoppingToken);
                    }
                    else
                    {
                        logger.LogWarning("Received invalid message: {MessageId}", message.MessageId);
                    }

                    await queueStorage.DeleteMessageAsync(QueueNames.MessageSend, message.MessageId, message.PopReceipt, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Error processing message queue");
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
        }
    }
}
