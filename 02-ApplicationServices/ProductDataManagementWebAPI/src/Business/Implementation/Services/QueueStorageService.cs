using Azure.Core;
using Azure.Identity;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Business.Interfaces.Configurations;
using Business.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Business.Implementation.Services
{
    public sealed class QueueStorageService : IQueueStorageService
    {
        private readonly QueueServiceClient queueServiceClient;
        private readonly BlobStorageSettings settings; // reuse same account URL
        private readonly ILogger<QueueStorageService> logger;

        public QueueStorageService(IOptions<BlobStorageSettings> options, ILogger<QueueStorageService> logger)
        {
            settings = options.Value;
            this.logger = logger;

            if (string.IsNullOrWhiteSpace(settings.QueueUrl))
            {
                throw new ArgumentException("BlobStorage:Url is not configured.");
            }

            var queueUri = new Uri(settings.QueueUrl);

            TokenCredential credential = new DefaultAzureCredential();
            queueServiceClient = new QueueServiceClient(queueUri, credential);
        }

        public async Task EnsureQueueAsync(string queueName, CancellationToken cancellationToken = default)
        {
            var queue = queueServiceClient.GetQueueClient(queueName);
            await queue.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        }

        public async Task EnqueueAsync(string queueName, string messageText, TimeSpan? visibilityTimeout = null, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentException("queueName is required");
            }

            if (string.IsNullOrEmpty(messageText))
            {
                throw new ArgumentException("messageText is required");
            }

            var queue = queueServiceClient.GetQueueClient(queueName);
            await queue.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            await queue.SendMessageAsync(messageText, visibilityTimeout, timeToLive, cancellationToken);
            logger.LogInformation("Enqueued message to {Queue}", queueName);
        }

        public async Task<DequeuedMessage?> DequeueAsync(string queueName, TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default)
        {
            var queue = queueServiceClient.GetQueueClient(queueName);
            await queue.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            QueueMessage[] messages = (await queue.ReceiveMessagesAsync(1, visibilityTimeout, cancellationToken)).Value;
            var msg = messages.FirstOrDefault();
            if (msg == null)
            {
                return null;
            }

            return new DequeuedMessage
            {
                MessageId = msg.MessageId,
                PopReceipt = msg.PopReceipt,
                Text = msg.MessageText,
                NextVisibleOn = msg.NextVisibleOn,
                DequeueCount = msg.DequeueCount
            };
        }

        public async Task<IReadOnlyList<string>> PeekAsync(string queueName, int maxMessages = 1, CancellationToken cancellationToken = default)
        {
            var queue = queueServiceClient.GetQueueClient(queueName);
            await queue.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            PeekedMessage[] messages = (await queue.PeekMessagesAsync(maxMessages, cancellationToken)).Value;
            return messages.Select(m => m.MessageText).ToArray();
        }

        public async Task DeleteMessageAsync(string queueName, string messageId, string popReceipt, CancellationToken cancellationToken = default)
        {
            var queue = queueServiceClient.GetQueueClient(queueName);
            await queue.DeleteMessageAsync(messageId, popReceipt, cancellationToken);
        }
    }
}
