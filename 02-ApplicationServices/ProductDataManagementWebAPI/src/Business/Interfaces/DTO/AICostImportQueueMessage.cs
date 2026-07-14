namespace Business.Interfaces.DTO
{
    public sealed class AICostImportQueueMessage
    {
        public required Guid BatchId { get; init; }
        public required Guid ItemId { get; init; }
    }
}
