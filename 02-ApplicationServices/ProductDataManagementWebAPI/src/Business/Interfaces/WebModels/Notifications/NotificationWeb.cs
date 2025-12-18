using Business.Interfaces.DTO;

namespace Business.Interfaces.WebModels.Notifications
{
    public record NotificationWeb(
        Guid Id,
        Guid TenantId,
        Guid? ProjectId,
        string TenantName,
        string? ProjectName,
        Guid UserId,
        NotificationType Type,
        string Title,
        string Message,
        DateTimeOffset CreatedAt,
        bool Readed,
        Dictionary<string, object?>? Metadata
    );
}