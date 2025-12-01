namespace Business.Interfaces.Services
{
    public interface IQueueStorageService
    {
        Task EnsureQueueAsync(string queueName, CancellationToken cancellationToken = default);
        Task EnqueueAsync(string queueName, string messageText, TimeSpan? visibilityTimeout = null, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default);
        Task<DequeuedMessage?> DequeueAsync(string queueName, TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> PeekAsync(string queueName, int maxMessages = 1, CancellationToken cancellationToken = default);
        Task DeleteMessageAsync(string queueName, string messageId, string popReceipt, CancellationToken cancellationToken = default);
    }

    public sealed class DequeuedMessage
    {
        public required string MessageId { get; init; }
        public required string PopReceipt { get; init; }
        public required string Text { get; init; }
        public DateTimeOffset? NextVisibleOn { get; init; }
        public long DequeueCount { get; init; }
    }
}
