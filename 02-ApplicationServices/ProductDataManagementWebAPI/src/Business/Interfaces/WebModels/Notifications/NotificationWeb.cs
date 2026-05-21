using Business.Interfaces.DTO;

namespace Business.Interfaces.WebModels.Notifications
{
    public sealed record NotificationWeb
    {
        public required Guid Id { get; init; }
        public required Guid TenantId { get; init; }
        public Guid? ProjectId { get; init; }
        public required string TenantName { get; init; }
        public string? ProjectName { get; init; }
        public required Guid UserId { get; init; }
        public required NotificationType Type { get; init; }
        public required string Title { get; init; }
        public required string Message { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
        public required bool IsRead { get; init; }
        public IReadOnlyDictionary<string, object?>? Metadata { get; init; }
    }
}
